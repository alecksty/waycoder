using System.Xml.Serialization;

namespace GenDev.Models
{
    [XmlRoot("Device")]
    public class DeviceModel
    {
        [XmlElement("Metadata")]
        public Metadata Metadata { get; set; } = new Metadata();
        
        [XmlElement("CPU")]
        public Cpu Cpu { get; set; } = new Cpu();
        
        [XmlElement("Memory")]
        public Memory Memory { get; set; } = new Memory();
        
        [XmlElement("Peripherals")]
        public Peripherals? Peripherals { get; set; }
        
        [XmlElement("Interrupts")]
        public Interrupts? Interrupts { get; set; }
        
        [XmlElement("Pins")]
        public Pins? Pins { get; set; }
    }

    public class Metadata
    {
        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;
        
        [XmlElement("Description")]
        public string Description { get; set; } = string.Empty;
        
        [XmlElement("Manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;
        
        [XmlElement("Family")]
        public string Family { get; set; } = string.Empty;
        
        [XmlElement("Series")]
        public string Series { get; set; } = string.Empty;
        
        [XmlElement("Version")]
        public string Version { get; set; } = string.Empty;
        
        [XmlElement("Author")]
        public string Author { get; set; } = string.Empty;
        
        [XmlElement("Date")]
        public string Date { get; set; } = string.Empty;
        
        [XmlElement("License")]
        public string? License { get; set; }
        
        [XmlElement("Website")]
        public string? Website { get; set; }
        
        [XmlElement("Datasheet")]
        public string? Datasheet { get; set; }
    }

    public class Cpu
    {
        [XmlElement("Architecture")]
        public string Architecture { get; set; } = string.Empty;
        
        [XmlElement("Bits")]
        public int Bits { get; set; }
        
        [XmlElement("Endianness")]
        public string? Endianness { get; set; }
        
        [XmlElement("Clock")]
        public Clock Clock { get; set; } = new Clock();
        
        [XmlElement("Registers")]
        public Registers Registers { get; set; } = new Registers();
    }

    public class Clock
    {
        [XmlElement("Min")]
        public string Min { get; set; } = "0";
        
        [XmlElement("Max")]
        public string Max { get; set; } = "0";
        
        [XmlElement("Default")]
        public string Default { get; set; } = "0";
        
        [XmlElement("Unit")]
        public string Unit { get; set; } = "Hz";
    }

    public class Registers
    {
        [XmlElement("Register")]
        public List<Register> RegisterList { get; set; } = new List<Register>();
    }

    public class Register
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;
        
        [XmlAttribute("address")]
        public string Address { get; set; } = string.Empty;
        
        [XmlAttribute("size")]
        public int Size { get; set; } = 1;
        
        [XmlAttribute("access")]
        public string Access { get; set; } = "rw";
        
        [XmlAttribute("description")]
        public string? Description { get; set; }
        
        [XmlElement("BitField")]
        public List<BitField> BitFields { get; set; } = new List<BitField>();
    }

    public class BitField
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;
        
        [XmlAttribute("bit")]
        public int Bit { get; set; }
        
        [XmlAttribute("description")]
        public string? Description { get; set; }
    }

    public class Memory
    {
        [XmlElement("Segment")]
        public List<Segment> Segments { get; set; } = new List<Segment>();
    }

    public class Segment
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;
        
        [XmlAttribute("type")]
        public string Type { get; set; } = string.Empty;
        
        [XmlAttribute("start")]
        public string Start { get; set; } = string.Empty;
        
        [XmlAttribute("end")]
        public string End { get; set; } = string.Empty;
        
        [XmlAttribute("size")]
        public string Size { get; set; } = "0";
        
        [XmlAttribute("description")]
        public string? Description { get; set; }
    }

    public class Peripherals
    {
        [XmlElement("Peripheral")]
        public List<Peripheral> PeripheralList { get; set; } = new List<Peripheral>();
    }

    public class Peripheral
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;
        
        [XmlAttribute("type")]
        public string Type { get; set; } = string.Empty;
        
        [XmlAttribute("base")]
        public string Base { get; set; } = string.Empty;
        
        [XmlAttribute("description")]
        public string? Description { get; set; }
        
        [XmlElement("Register")]
        public List<Register> Registers { get; set; } = new List<Register>();
        
        [XmlElement("BitField")]
        public List<BitField> BitFields { get; set; } = new List<BitField>();
    }

    public class Interrupts
    {
        [XmlElement("Interrupt")]
        public List<Interrupt> InterruptList { get; set; } = new List<Interrupt>();
    }

    public class Interrupt
    {
        [XmlAttribute("vector")]
        public int Vector { get; set; }
        
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;
        
        [XmlAttribute("description")]
        public string? Description { get; set; }
    }

    public class Pins
    {
        [XmlElement("Pin")]
        public List<Pin> PinList { get; set; } = new List<Pin>();
    }

    public class Pin
    {
        [XmlAttribute("number")]
        public int Number { get; set; }
        
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;
        
        [XmlAttribute("type")]
        public string Type { get; set; } = string.Empty;
        
        [XmlAttribute("description")]
        public string? Description { get; set; }
    }
}