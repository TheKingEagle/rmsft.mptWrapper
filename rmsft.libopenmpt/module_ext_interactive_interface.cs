using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace rmsft.mptWrapper
{
    [StructLayout(LayoutKind.Sequential)]
    public struct openmpt_module_ext_interface_interactive
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetCurrentSpeedDelegate(IntPtr mod_ext, int speed);
        public SetCurrentSpeedDelegate set_current_speed;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetCurrentTempoDelegate(IntPtr mod_ext, int tempo);
        public SetCurrentTempoDelegate set_current_tempo;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetTempoFactorDelegate(IntPtr mod_ext, double factor);
        public SetTempoFactorDelegate set_tempo_factor;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double GetTempoFactorDelegate(IntPtr mod_ext);
        public GetTempoFactorDelegate get_tempo_factor;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetPitchFactorDelegate(IntPtr mod_ext, double factor);
        public SetPitchFactorDelegate set_pitch_factor;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double GetPitchFactorDelegate(IntPtr mod_ext);
        public GetPitchFactorDelegate get_pitch_factor;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetGlobalVolumeDelegate(IntPtr mod_ext, double volume);
        public SetGlobalVolumeDelegate set_global_volume;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double GetGlobalVolumeDelegate(IntPtr mod_ext);
        public GetGlobalVolumeDelegate get_global_volume;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetChannelVolumeDelegate(IntPtr mod_ext, int channel, double volume);
        public SetChannelVolumeDelegate set_channel_volume;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double GetChannelVolumeDelegate(IntPtr mod_ext, int channel);
        public GetChannelVolumeDelegate get_channel_volume;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetChannelMuteStatusDelegate(IntPtr mod_ext, int channel, int mute);
        public SetChannelMuteStatusDelegate set_channel_mute_status;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetChannelMuteStatusDelegate(IntPtr mod_ext, int channel);
        public GetChannelMuteStatusDelegate get_channel_mute_status;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetInstrumentMuteStatusDelegate(IntPtr mod_ext, int instrument, int mute);
        public SetInstrumentMuteStatusDelegate set_instrument_mute_status;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetInstrumentMuteStatusDelegate(IntPtr mod_ext, int instrument);
        public GetInstrumentMuteStatusDelegate get_instrument_mute_status;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int PlayNoteDelegate(IntPtr mod_ext, int instrument, int note, double volume, double panning);
        public PlayNoteDelegate play_note;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int StopNoteDelegate(IntPtr mod_ext, int channel);
        public StopNoteDelegate stop_note;
    }
}
