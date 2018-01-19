using System.Configuration;

namespace CleanerLogs
{
  public class MachinesConfigSection : ConfigurationSection
  {

    [ConfigurationProperty("Machines")]
    public MachinesCollection MachineItems
    {
      get { return (MachinesCollection) base["Machines"]; }
    }
  }

  public class MachineElement : ConfigurationElement
  {
    [ConfigurationProperty("number", DefaultValue = "", IsKey = true, IsRequired = true)]
    public string MachineNumber
    {
      get { return (string)base["number"]; }
      set { base["number"] = value; }
    }

    [ConfigurationProperty("ip", DefaultValue = "", IsKey = false, IsRequired = false)]
    public string MachineIp
    {
      get { return (string)base["ip"]; }
      set { base["ip"] = value; }
    }
  }

  [ConfigurationCollection(typeof(MachineElement), AddItemName = "Machine")]
  public class MachinesCollection : ConfigurationElementCollection
  {
    protected override ConfigurationElement CreateNewElement()
    {
      return new MachineElement();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
      return ((MachineElement) element).MachineIp;
    }

    public MachineElement this[int index]
    {
      get { return (MachineElement) BaseGet(index); }
    }
  }
}
