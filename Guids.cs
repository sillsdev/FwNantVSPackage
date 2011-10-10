// Guids.cs
// MUST match guids.h
using System;

namespace SIL.FwNantVSPackage
{
    static class GuidList
    {
        public const string guidFwNantVSPackagePkgString = "60ea12d0-6167-46c7-b425-5dafb230b5f9";
        public const string guidFwNantVSPackageCmdSetString = "fed8a74b-0203-46c4-a2b9-f4a63711e1f3";

        public static readonly Guid guidFwNantVSPackageCmdSet = new Guid(guidFwNantVSPackageCmdSetString);
    };
}