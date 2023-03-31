using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rmsft.mptWrapper
{
    using NAudio.Wave;
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using static MPTInterops;
    public static class ModuleAPI
    {
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
            
            openmpt_module_set_repeat_count(mod_std, -1);
            // Initialize the output device and stream provider
            var waveOut = new WaveOutEvent();
            waveOut.DesiredLatency = 60;
            var waveProvider = new BufferedWaveProvider(new WaveFormat(44100,16,2));
            waveProvider.DiscardOnBufferOverflow = false;
            waveProvider.ReadFully = true;
            waveProvider.BufferDuration = TimeSpan.FromMilliseconds(60);
           
            // Start the playback loop

            while (true)
            {
                // Read the next stereo sample block from the module
                var buffer = new short[4096];
                //Why is this the way it is? 44100 sample rate, but 2 channels? and what is count?
                var samplesRead = openmpt_module_read_interleaved_stereo(mod_std,88200, 2, buffer);

                // If there are no more samples to read, restart the subsong, but this is just debug.
                if (samplesRead == 0)
                {
                    Console.WriteLine("DONE");

                    openmpt_module_set_position_seconds(mod_std, 0);
                    continue;
                }
                
                var bytes = new byte[samplesRead * sizeof(short)];
                Buffer.BlockCopy(buffer, 0, bytes, 0, bytes.Length);

                // Send the sample buffer to the output stream, but wait until buffer has free space.
                // I am probably doing this wrong.
                SpinWait.SpinUntil(() => waveProvider.BufferLength - waveProvider.BufferedBytes > bytes.Length);
                
                waveProvider.AddSamples(bytes, 0, bytes.Length);

                if (waveOut.PlaybackState != PlaybackState.Playing)
                {
                    waveOut.Init(waveProvider);
                    waveOut.Play();
                }

            }

        }


        #region libopenmpt-ext Interactives
        /// <summary>
        /// TODO: Fade out a channel. (it just hard sets 0 right now)
        /// Pattern takes priority; ie. if pattern has channel volume parameters, those get used, and will override fade.
        /// </summary>
        /// <param name="mod_ext">The handle to the extended module</param>
        /// <param name="channel">channel range from 0 to (however many channels in the module)</param>
        public static void ChannelFadeOut(IntPtr mod_ext, int channel)
        {
            IntPtr interfacePtr = Marshal.AllocHGlobal(Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            bool result = openmpt_module_ext_get_interface(mod_ext, "interactive", interfacePtr, (UIntPtr)Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            if (result)
            {
                Console.WriteLine("Success? {0}", channel);
                openmpt_module_ext_interface_interactive interfaceData = Marshal.PtrToStructure<openmpt_module_ext_interface_interactive>(interfacePtr);
                interfaceData.set_channel_volume(mod_ext, channel, 0);

                for (int i = 0; i < 17; i++)
                {
                    Console.WriteLine("New vol channel {0}: {1}", i, interfaceData.get_channel_volume(mod_ext, i));
                }
            }
            Marshal.FreeHGlobal(interfacePtr);
        }

        /// <summary>
        /// TODO: Fade in a channel. (it just hard sets 1 right now)
        /// Pattern takes priority; ie. if pattern has channel volume parameters, those get used, and will override fade.
        /// </summary>
        /// <param name="mod_ext">The handle to the extended module</param>
        /// <param name="channel">channel range from 0 to (however many channels in the module)</param>
        public static void ChannelFadeIn(IntPtr mod_ext, int channel)
        {
            IntPtr interfacePtr = Marshal.AllocHGlobal(Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            bool result = openmpt_module_ext_get_interface(mod_ext, "interactive", interfacePtr, (UIntPtr)Marshal.SizeOf<openmpt_module_ext_interface_interactive>());
            if (result)
            {
                Console.WriteLine("Success? {0}", channel);
                openmpt_module_ext_interface_interactive interfaceData = Marshal.PtrToStructure<openmpt_module_ext_interface_interactive>(interfacePtr);
                interfaceData.set_channel_volume(mod_ext, channel, 1);

                for (int i = 0; i < 17; i++)
                {
                    Console.WriteLine("New vol channel {0}: {1}", i, interfaceData.get_channel_volume(mod_ext, i));
                }
            }
            Marshal.FreeHGlobal(interfacePtr);
        }

        #endregion


        #region libopenmpt interactives
        public static bool SetSubSong(IntPtr mod_std, int index)
        {
            return openmpt_module_select_subsong(mod_std, index);
        }

        public static void SetOrderRow(IntPtr mod_std, int order, int row)
        {
            openmpt_module_set_position_order_row(mod_std, order, row);
        }

        #endregion
    }

}
