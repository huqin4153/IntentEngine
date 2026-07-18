using System;
using System.Collections.Generic;
using System.Configuration;

namespace IntentEngine.ConfigSections
{
    public class LoginAccountSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public AccountElementCollection Accounts =>
            (AccountElementCollection)this[""];

        public List<AccountInfo> GetAccounts()
        {
            var list = new List<AccountInfo>();
            foreach (AccountElement item in Accounts)
            {
                list.Add(new AccountInfo
                {
                    Username = item.Username,
                    Password = item.Password,
                    Role = item.Role
                });
            }
            return list;
        }
    }

    public class AccountElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() => new AccountElement();

        protected override object GetElementKey(ConfigurationElement element) =>
            ((AccountElement)element).Username;

        public override ConfigurationElementCollectionType CollectionType =>
            ConfigurationElementCollectionType.BasicMap;
    }

    public class AccountElement : ConfigurationElement
    {
        [ConfigurationProperty("username", IsRequired = true, IsKey = true)]
        public string Username => (string)this["username"];

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password => (string)this["password"];

        [ConfigurationProperty("role", DefaultValue = "操作员")]
        public string Role => (string)this["role"];
    }

    public class AccountInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}
