using System.ComponentModel;
using System.Xml.Serialization;

namespace iOSBot.Service.Responses;

// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
[XmlRoot(Namespace = "", IsNullable = false)]
public partial class RssResponse
{
    private rssChannel channelField;

    private decimal versionField;

    /// <remarks/>
    public rssChannel channel
    {
        get { return this.channelField; }
        set { this.channelField = value; }
    }

    /// <remarks/>
    [XmlAttribute()]
    public decimal version
    {
        get { return this.versionField; }
        set { this.versionField = value; }
    }
}

/// <remarks/>
[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class rssChannel
{
    private link linkField;

    private string titleField;

    private string link1Field;

    private string descriptionField;

    private string languageField;

    private string lastBuildDateField;

    private string generatorField;

    private string copyrightField;

    private rssChannelItem[] itemField;

    /// <remarks/>
    [XmlElement(Namespace = "http://www.w3.org/2005/Atom")]
    public link link
    {
        get { return this.linkField; }
        set { this.linkField = value; }
    }

    /// <remarks/>
    public string title
    {
        get { return this.titleField; }
        set { this.titleField = value; }
    }

    /// <remarks/>
    [XmlElement("link")]
    public string link1
    {
        get { return this.link1Field; }
        set { this.link1Field = value; }
    }

    /// <remarks/>
    public string description
    {
        get { return this.descriptionField; }
        set { this.descriptionField = value; }
    }

    /// <remarks/>
    public string language
    {
        get { return this.languageField; }
        set { this.languageField = value; }
    }

    /// <remarks/>
    public string lastBuildDate
    {
        get { return this.lastBuildDateField; }
        set { this.lastBuildDateField = value; }
    }

    /// <remarks/>
    public string generator
    {
        get { return this.generatorField; }
        set { this.generatorField = value; }
    }

    /// <remarks/>
    public string copyright
    {
        get { return this.copyrightField; }
        set { this.copyrightField = value; }
    }

    /// <remarks/>
    [XmlElement("item")]
    public rssChannelItem[] item
    {
        get { return this.itemField; }
        set { this.itemField = value; }
    }
}

/// <remarks/>
[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.w3.org/2005/Atom")]
[XmlRoot(Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
public partial class link
{
    private string hrefField;

    private string relField;

    private string typeField;

    /// <remarks/>
    [XmlAttribute()]
    public string href
    {
        get { return this.hrefField; }
        set { this.hrefField = value; }
    }

    /// <remarks/>
    [XmlAttribute()]
    public string rel
    {
        get { return this.relField; }
        set { this.relField = value; }
    }

    /// <remarks/>
    [XmlAttribute()]
    public string type
    {
        get { return this.typeField; }
        set { this.typeField = value; }
    }
}

/// <remarks/>
[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class rssChannelItem
{
    private string titleField;

    private string linkField;

    private string guidField;

    private string descriptionField;

    private string pubDateField;

    private string encodedField;

    /// <remarks/>
    public string title
    {
        get { return this.titleField; }
        set { this.titleField = value; }
    }

    /// <remarks/>
    public string link
    {
        get { return this.linkField; }
        set { this.linkField = value; }
    }

    /// <remarks/>
    public string guid
    {
        get { return this.guidField; }
        set { this.guidField = value; }
    }

    /// <remarks/>
    public string description
    {
        get { return this.descriptionField; }
        set { this.descriptionField = value; }
    }

    /// <remarks/>
    public string pubDate
    {
        get { return this.pubDateField; }
        set { this.pubDateField = value; }
    }

    /// <remarks/>
    [XmlElement(Namespace = "http://purl.org/rss/1.0/modules/content/")]
    public string encoded
    {
        get { return this.encodedField; }
        set { this.encodedField = value; }
    }
}