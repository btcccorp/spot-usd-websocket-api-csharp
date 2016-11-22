
namespace WSAPItest
{
	class Credentials : Credentials.ICredentials
	{
		public string Account { get; private set; }
		public string Key { get; private set; }

	    internal interface ICredentials
	    {
	        string Account { get; }
            string Key { get; }
	    }

	    internal class AccessKey : ICredentials
	    {
	        public AccessKey(string access_key, string secret_key)
	        {
	            Account = access_key;
	            Key = secret_key;
	        }

            public string Account { get; private set; }
            public string Key { get; private set; }
	    }
	}
}
