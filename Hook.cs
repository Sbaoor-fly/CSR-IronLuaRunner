using System;
using CSR;
using System.Runtime.InteropServices;


namespace IronPythonRunner
{
	class Hook
	{
		private delegate void TP(IntPtr pl, Vec3 pos, int a1, int dimid, int a2, int a3, int a4, ulong uid);
		public static void tp(MCCSAPI api, int funcaddr, IntPtr pl, Vec3 pos, int dimid)
		{
			var functpr = api.dlsym(funcaddr);
			var _tp = (TP)Marshal.GetDelegateForFunctionPointer(functpr, typeof(TP));
			_tp(pl, pos, 0, dimid, 0, 0, 0, new CsPlayer(api, pl).UniqueId);
			/*?teleport@TeleportCommand@@SAXAEAVActor@@VVec3@@PEAV3@V?$AutomaticID@VDimension@@H@@VRelativeFloat@@4HAEBUActorUniqueID@@@Z*/
		}
	}
}
