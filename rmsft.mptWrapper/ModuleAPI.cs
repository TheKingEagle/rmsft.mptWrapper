using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rmsft.mptWrapper
{
    using NAudio.Wave;
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using static MPTInterops;
    //TODO: Write a module object for OOP paradigm
    public static class ModuleAPI
    {
        public static event EventHandler<PatternEventArgs> PatternStarted;
        public static event EventHandler<PatternEventArgs> PatternEnded;
        public static event EventHandler<PatternEventArgs> PatternRowChanged;
        private static Mutex moduleMutex = new Mutex();
        private static bool firedPatstart = false;
        private static bool firedPatEnd = false;

        /// <summary>
        /// Load an openMPT module from file
        /// </summary>
        /// <param name="filepath">path on filesystem</param>
        /// <returns>Handle to the new mod_ext</returns>
        public static IntPtr LoadFromFile(string filepath)
        {

            byte[] moduleData = System.IO.File.ReadAllBytes(filepath);
            // Allocate a block of memory in unmanaged space to hold the module data
            IntPtr moduleDataPtr = Marshal.AllocHGlobal(moduleData.Length);

            // Copy the module data from managed to unmanaged memory
            Marshal.Copy(moduleData, 0, moduleDataPtr, moduleData.Length);

            // Call the openmpt_module_create_from_memory function to create the module
            IntPtr module = openmpt_module_ext_create_from_memory(moduleDataPtr, (uint)moduleData.Length,null,IntPtr.Zero,null,IntPtr.Zero,out int error,out IntPtr msg, new openmpt_module_initial_ctl());

            // Free the memory allocated for the module data
            Marshal.FreeHGlobal(moduleDataPtr);

            return module;
        }

        /// <summary>
        /// Load an openMPT module from byte array
        /// </summary>
        /// <param name="filepath">path on filesystem</param>
        /// <returns>Handle to the new mod_ext</returns>
        public static IntPtr LoadFromBuffer(byte[] moduleData)
        {

            
            // Allocate a block of memory in unmanaged space to hold the module data
            IntPtr moduleDataPtr = Marshal.AllocHGlobal(moduleData.Length);

            // Copy the module data from managed to unmanaged memory
            Marshal.Copy(moduleData, 0, moduleDataPtr, moduleData.Length);

            // Call the openmpt_module_create_from_memory function to create the module
            IntPtr module = openmpt_module_ext_create_from_memory(moduleDataPtr, (uint)moduleData.Length, null, IntPtr.Zero, null, IntPtr.Zero, out int error, out IntPtr msg, new openmpt_module_initial_ctl());

            // Free the memory allocated for the module data
            Marshal.FreeHGlobal(moduleDataPtr);

            return module;
        }

        /// <summary>
        /// Creates a standard module for use in standard openMPT methods.
        /// </summary>
        /// <param name="mod_ext">the handle to the module loaded.</param>
        /// <returns></returns>
        public static IntPtr GetStdModule(IntPtr mod_ext)
        {
            return openmpt_module_ext_get_module(mod_ext);
        }

        /// <summary>
        /// Starts a module stream that loops indefinitely, on the calling thread.
        /// 
        /// NOTE: buffering is working (on this machine) but is wack. I am probably doing it wrong :)
        /// </summary>
        /// <param name="mod_std">openMPT standard module handle</param>
        /// <param name="subsongIndex">which sub-song to start on</param>
        public static void StartModuleStream(IntPtr mod_std, int subsongIndex)
        {
            openmpt_module_select_subsong(mod_std, subsongIndex);
            openmpt_module_set_repeat_count(mod_std, -1);
            openmpt_module_ctl_set_boolean(mod_std, "seek.sync_samples", true);
            openmpt_module_ctl_set_text(mod_std, "play.at_end", "stop");
            // Initialize the output device and stream provider
            var waveOut = new WaveOutEvent();
            waveOut.DesiredLatency = 60;
            var waveProvider = new BufferedWaveProvider(new WaveFormat(44100,2));
            waveProvider.DiscardOnBufferOverflow = false;
            waveProvider.ReadFully = true;
            waveProvider.BufferDuration = TimeSpan.FromMilliseconds(60);
            int lrow = -1;
            // Start the playback loop

            while (true)
            {
                moduleMutex.WaitOne();

                // Read the next stereo sample block from the module
                var buffer = new short[4];
                //Why is this the way it is? 44100 sample rate, but 2 channels? and what is count?
                var samplesRead = openmpt_module_read_interleaved_stereo(mod_std,44100*2, 2, buffer);

                int row = openmpt_module_get_current_row(mod_std);

                if(row == 0 && !firedPatstart)
                {
                    Task.Run(()=>PatternStarted?.Invoke(mod_std, new PatternEventArgs(openmpt_module_get_current_pattern(mod_std), openmpt_module_get_current_order(mod_std),row,openmpt_module_get_selected_subsong(mod_std))));
                    firedPatstart = true;
                }
                if(row > 0)
                {
                    firedPatstart = false;
                }
                int lastRowindex = GetPatternRowCount(mod_std, openmpt_module_get_current_pattern(mod_std)) - 1;
                if (row ==  lastRowindex&& !firedPatEnd)
                {
                    Task.Run(()=>PatternEnded?.Invoke(mod_std, new PatternEventArgs(openmpt_module_get_current_pattern(mod_std), openmpt_module_get_current_order(mod_std),row,openmpt_module_get_selected_subsong(mod_std))));

                    firedPatEnd = true;
                }
                else
                {
                    firedPatEnd = false;
                }

                if(row != lrow)
                {
                    Task.Run(() => PatternRowChanged?.Invoke(mod_std, new PatternEventArgs(openmpt_module_get_current_pattern(mod_std), openmpt_module_get_current_order(mod_std), row, openmpt_module_get_selected_subsong(mod_std))));
                    lrow = row;

                }

                // If there are no more samples to read, restart the subsong, but this is just debug.
                if (samplesRead == 0)
                {
                    Console.WriteLine("DONE");

                    openmpt_module_set_position_seconds(mod_std, 0);
                    break;
                }
                
                var bytes = new byte[samplesRead * sizeof(short)];
                Buffer.BlockCopy(buffer, 0, bytes, 0, bytes.Length);

                // Send the sample buffer to the output stream, but wait until buffer has free space.
                // Seeing as how if this isn't here, the buffer will be full.
                SpinWait.SpinUntil(() => waveProvider.BufferLength - waveProvider.BufferedBytes > bytes.Length);
                
                waveProvider.AddSamples(bytes, 0, bytes.Length);

                if (waveOut.PlaybackState != PlaybackState.Playing)
                {
                    waveOut.Init(waveProvider);
                    waveOut.Play();
                }
                moduleMutex.ReleaseMutex();
            }

        }

        #region libopenmpt-ext Interactives


        /// <summary>
        /// Fade the volume of the channel to the target volume.
        /// </summary>
        /// <param name="mod_ext">The handle to the extended module</param>
        /// <param name="channel">channel index</param>
        /// <param name="targetVolume">a valid number between 0 and 1</param>
        /// <param name="durationInSeconds">a value greater than zero.</param>
        /// <returns></returns>
        public static double FadeChannelVolume(IntPtr mod_ext, int channel, double targetVolume, double fade_seconds)
        {
            if (fade_seconds <= 0)
            {
                fade_seconds = 0.001;
            }

            IntPtr interfacePtr = Marshal.AllocHGlobal(Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            bool result = openmpt_module_ext_get_interface(mod_ext, "interactive", interfacePtr, (UIntPtr)Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            if (result)
            {

                openmpt_module_ext_interface_interactive interfaceData = Marshal.PtrToStructure<openmpt_module_ext_interface_interactive>(interfacePtr);
                // Calculate volume increment per millisecond
                double OldVolume = interfaceData.get_channel_volume(mod_ext, channel);
                if(targetVolume - OldVolume == 0)
                {
                    Console.WriteLine("Target volume isn't different.");
                    return OldVolume;
                }
                double active = OldVolume;
                double volumeIncrement = (targetVolume - OldVolume) / (fade_seconds * 1000);
                int interval = 60; // in milliseconds
                double incrementPerInterval = volumeIncrement * interval;

                // Start a timer to update the volume at regular intervals
                Stopwatch s = new Stopwatch();
                s.Start();
                Timer tt = null;
                tt = new Timer(_ => 
                {
                    moduleMutex.WaitOne();
                    double currentVolume = active;
                    double newVolume = currentVolume + incrementPerInterval;
                    if (volumeIncrement > 0 && newVolume > targetVolume || volumeIncrement < 0 && newVolume < targetVolume)
                    {
                        newVolume = targetVolume;
                        tt.Dispose();
                        s.Stop();
                        Console.WriteLine("Channel fade completed in {0} ms", s.ElapsedMilliseconds);
                    }
                    active = newVolume;
                    interfaceData.set_channel_volume(mod_ext, channel, active);
                    moduleMutex.ReleaseMutex();
                },null,0,interval);
                Marshal.FreeHGlobal(interfacePtr);
                return OldVolume;
            }
            else
            {
                return 1;//default 1 if something blows up.
            }
        }

        /// <summary>
        /// Switch songs but fade out the current song.
        /// Pattern commands take priority here.
        /// </summary>
        /// <param name="mod_std">handle to your module.</param>
        /// <param name="mod_ext">handle to your ext-module.</param>
        /// <param name="target">the target song index.</param>
        /// <param name="fade_seconds">how long in seconds the fade should be.</param>
        public static void FadeToSubSong(IntPtr mod_std, IntPtr mod_ext, int target,double fade_seconds)
        {
            
            IntPtr interfacePtr = Marshal.AllocHGlobal(Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            bool result = openmpt_module_ext_get_interface(mod_ext, "interactive", interfacePtr, (UIntPtr)Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            if (result)
            {

                openmpt_module_ext_interface_interactive interfaceData = Marshal.PtrToStructure<openmpt_module_ext_interface_interactive>(interfacePtr);

                // Calculate volume increment per millisecond
                double OldVolume = 1;
                double active = OldVolume;
                double volumeIncrement = (0 - OldVolume) / (fade_seconds * 1000);
                int interval = 60; // in milliseconds
                double incrementPerInterval = volumeIncrement * interval;
                Stopwatch s = new Stopwatch();
                // Start a timer to update the volume at regular intervals
                Timer tt = null;
                 tt = new Timer(_=> {
                    double currentVolume = active;
                    double newVolume = currentVolume + incrementPerInterval;
                    if (volumeIncrement <= 0 && newVolume <= 0)
                    {

                        moduleMutex.WaitOne();
                        openmpt_module_select_subsong(mod_std, target);
                        interfaceData.set_global_volume(mod_ext, 1);
                        moduleMutex.ReleaseMutex();
                        s.Stop();
                        Console.WriteLine("Executed fadeout in {0} ms", s.ElapsedMilliseconds);
                        tt.Dispose();
                    }
                    else
                    {
                        active = newVolume;
                        moduleMutex.WaitOne();
                        interfaceData.set_global_volume(mod_ext, active);
                        moduleMutex.ReleaseMutex();


                    }

                },null, 0, interval);
                
                s.Start();
                Marshal.FreeHGlobal(interfacePtr);
            }
        }


        /// <summary>
        /// Switch songs at specific position but fade out first.
        /// Pattern commands take priority here.
        /// </summary>
        /// <param name="mod_std">handle to your module.</param>
        /// <param name="mod_ext">handle to your ext-module.</param>
        /// <param name="songIndex">the target song index.</param>
        /// <param name="order">offset pattern order.</param>
        /// <param name="fade_seconds">how long in seconds the fade should be.</param>
        public static void FadeToSubSong(IntPtr mod_std, IntPtr mod_ext, int songIndex,int order, double fade_seconds, int row = 0)
        {

            IntPtr interfacePtr = Marshal.AllocHGlobal(Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            bool result = openmpt_module_ext_get_interface(mod_ext, "interactive", interfacePtr, (UIntPtr)Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            if (result)
            {

                openmpt_module_ext_interface_interactive interfaceData = Marshal.PtrToStructure<openmpt_module_ext_interface_interactive>(interfacePtr);

                // Calculate volume increment per millisecond
                double OldVolume = 1;
                double active = OldVolume;
                double volumeIncrement = (0 - OldVolume) / (fade_seconds * 1000);
                int interval = 60; // in milliseconds
                double incrementPerInterval = volumeIncrement * interval;
                Stopwatch s = new Stopwatch();
                // Start a timer to update the volume at regular intervals
                Timer tt = null;
                tt = new Timer(_ => {
                    double currentVolume = active;
                    double newVolume = currentVolume + incrementPerInterval;
                    if (volumeIncrement <= 0 && newVolume <= 0)
                    {

                        moduleMutex.WaitOne();
                        openmpt_module_select_subsong(mod_std, songIndex);
                        openmpt_module_set_position_order_row(mod_std, order, row);
                        interfaceData.set_global_volume(mod_ext, 1);
                        moduleMutex.ReleaseMutex();
                        s.Stop();
                        Console.WriteLine("Executed fadeout in {0} ms", s.ElapsedMilliseconds);
                        tt.Dispose();
                    }
                    else
                    {
                        active = newVolume;
                        moduleMutex.WaitOne();
                        interfaceData.set_global_volume(mod_ext, active);
                        moduleMutex.ReleaseMutex();


                    }

                }, null, 0, interval);

                s.Start();
                Marshal.FreeHGlobal(interfacePtr);
            }
        }

        #endregion

        #region libopenmpt interactives
        public static bool SetSubSong(IntPtr mod_std, int index)
        {
            moduleMutex.WaitOne();
            bool f = openmpt_module_select_subsong(mod_std, index);
            moduleMutex.ReleaseMutex();
            return f;
        }

        public static void SetOrderRow(IntPtr mod_std, int order, int row)
        {
            moduleMutex.WaitOne();
            openmpt_module_set_position_order_row(mod_std, order, row);
            moduleMutex.ReleaseMutex();

        }

        #endregion

        #region Module Info
        public static int GetChannelCount(IntPtr mod_std) => openmpt_module_get_num_channels(mod_std);
        public static int GetSongCount(IntPtr mod_std) => openmpt_module_get_num_subsongs(mod_std);
        public static int GetPatternCount(IntPtr mod_std) => openmpt_module_get_num_patterns(mod_std);
        public static int GetPatternRowCount(IntPtr mod_std,int pattern_index) => openmpt_module_get_pattern_num_rows(mod_std,pattern_index);
        public static int GetOrderCount(IntPtr mod_std) => openmpt_module_get_num_orders(mod_std);
        public static int GetCurrentOrder(IntPtr mod_std) => openmpt_module_get_current_order(mod_std);
        public static int GetCurrentRow(IntPtr mod_std) => openmpt_module_get_current_row(mod_std);
        #endregion
    }

}
