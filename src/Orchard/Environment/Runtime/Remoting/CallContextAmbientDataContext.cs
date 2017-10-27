using System.Runtime.Remoting.Messaging;

namespace Orchard.Environment.Runtime.Remoting
{
    /// <summary>
    ///  �ṩ��ִ�д���·��һ���͵����Լ�
    /// </summary>
    public class CallContextAmbientDataContext : IAmbientDataContext, ISingletonDependency
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetData(string key, object value)
        {
            if (value == null)
            {
                CallContext.FreeNamedDataSlot(key);
                return;
            }

            CallContext.LogicalSetData(key, value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetData(string key)
        {
            return CallContext.LogicalGetData(key);
        }
    }
}