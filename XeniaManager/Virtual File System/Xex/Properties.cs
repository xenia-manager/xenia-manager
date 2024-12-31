using XeniaManager.VFS.Models;

namespace XeniaManager.VFS
{
    public static partial class XexUtility
    {
        private struct XexContext
        {
            public XexHeader Header;
            public XexSecurityInfo SecurityInfo;
            public XexExecution Execution;
        }
    }
}