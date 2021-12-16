using QHackCLR.Clr.Builders;
using QHackCLR.Clr.Builders.Helpers;
using QHackCLR.Dac.Interfaces;
using QHackCLR.Dac.Helpers;
using QHackCLR.DataTargets;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QHackCLR.Clr
{
	public class ClrRuntime
	{
		protected nuint BaseAddress { get; }
		protected IRuntimeHelper RuntimeHelper { get; }
		public ClrInfo ClrInfo { get; }
		public ClrHeap Heap { get; }
		public ClrRuntime(ClrInfo clrInfo, IRuntimeHelper helper, nuint baseAddress)
		{
			ClrInfo = clrInfo;
			RuntimeHelper = helper;
			BaseAddress = baseAddress;
			Heap = new ClrHeap(this, helper.HeapHelper);

		}
		public DataTarget DataTarget => ClrInfo.DataTarget;
		public DacLibrary DacLibrary => RuntimeHelper.DacLibrary;


		/// <summary>
		/// a.k.a MSCORLIB or SYSTEM.PRIVATE.CORELIB
		/// </summary>
		/// <param name="runtime"></param>
		/// <returns></returns>
		public ClrModule BaseClassLibrary => Heap.ObjectType.Module;

		private ClrAppDomain _AppDomain;
		/// <summary>
		/// Supports only one appdomain, CORE style.
		/// </summary>
		public ClrAppDomain AppDomain => _AppDomain ??= RuntimeHelper.GetAppDomain(RuntimeHelper.SOSDac.GetAppDomainList()[0]);
	}
}
