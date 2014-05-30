// This file is part of iRacingSDK.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingSDK.Net
//
// iRacingSDK is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingSDK is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingSDK.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Win32.Synchronization;

namespace iRacingSDK
{
	static class iRacingConnection
	{
		public static MemoryMappedViewAccessor Accessor {get; private set; }
		static IntPtr dataValidEvent;
		static MemoryMappedFile irsdkMappedMemory;

		public static bool IsConnected()
		{
			if(Accessor != null)
				return true;

			var dataValidEvent =  Event.OpenEvent(Event.EVENT_ALL_ACCESS | Event.EVENT_MODIFY_STATE, false, "Local\\IRSDKDataValidEvent");
            if (dataValidEvent == IntPtr.Zero)
            {
                var lastError = Marshal.GetLastWin32Error();
                Trace.WriteLine(string.Format("Unable to open event Local\\IRSDKDataValidEvent - Error Code {0}", lastError), "DEBUG");
                return false;
            }

            MemoryMappedFile irsdkMappedMemory = null;
            try
            {
                irsdkMappedMemory = MemoryMappedFile.OpenExisting("Local\\IRSDKMemMapFileName");
            }
            catch(Exception e)
            {
                Trace.WriteLine("Error accessing shared memory", "DEBUG");
                Trace.WriteLine(e.Message, "DEBUG");
            }

			if(irsdkMappedMemory == null)
				return false;

			var accessor = irsdkMappedMemory.CreateViewAccessor();
			if(accessor == null)
			{
				irsdkMappedMemory.Dispose();
                Trace.WriteLine("Unable to Create View into shared memory", "DEBUG");
				return false;
			}

			iRacingConnection.irsdkMappedMemory = irsdkMappedMemory;
			iRacingConnection.dataValidEvent = dataValidEvent;
			Accessor = accessor;
			return true;
		}

		public static bool WaitForData()
		{
            var result = Event.WaitForSingleObject(dataValidEvent, 30) == 0;
            Trace.WriteLineIf(!result, "Failed to get signal from iRacing for new Data Sample within 30 milliseconds", "DEBUG");
            return result;
		}
    }
}
