using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace rmsft.mptWrapper
{
    internal static class MPTInterops
    {

        // Import the libopenmpt library
        public const string libOpenMptPath = @"lib\\libopenmpt.dll";
        
        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool openmpt_module_ext_get_interface(IntPtr mod_ext, string interface_id, IntPtr interfacePtr, UIntPtr interface_size);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool openmpt_module_select_subsong(IntPtr mod, int subsong);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void openmpt_module_set_repeat_count(IntPtr module, int count);

        //Use this instead.
        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr openmpt_module_ext_create_from_memory(IntPtr filedata, uint filesize, openmpt_log_func logfunc, IntPtr loguser, openmpt_error_func errfunc, IntPtr erruser, out int error, out IntPtr error_message, openmpt_module_initial_ctl ctls);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr openmpt_module_ext_get_module(IntPtr ModuleEXT);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void openmpt_module_destroy(IntPtr module);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int openmpt_module_read_interleaved_stereo(IntPtr mod, int samplerate, int count, short[] buffer);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double openmpt_module_set_position_seconds(IntPtr module, double seconds);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double openmpt_module_set_position_order_row(IntPtr module, int order, int row);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int openmpt_module_get_num_channels(IntPtr module);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int openmpt_module_get_num_subsongs(IntPtr module);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int openmpt_module_get_num_orders(IntPtr module);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int openmpt_module_get_num_patterns(IntPtr module);

        [DllImport(libOpenMptPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int openmpt_module_get_pattern_num_rows(IntPtr module, int pattern);



        [StructLayout(LayoutKind.Sequential)]
        internal struct openmpt_module_initial_ctl
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string ctl;
            [MarshalAs(UnmanagedType.LPStr)]
            public string value;
        }

        internal delegate void openmpt_log_func(IntPtr mod, int level, [MarshalAs(UnmanagedType.LPStr)] string fmt, IntPtr arg);

        internal delegate void openmpt_error_func(IntPtr mod, int err, [MarshalAs(UnmanagedType.LPStr)] string fmt, IntPtr arg);

    }
}
