using Discord;

namespace iOSBot.Bot
{
    public static class Helpers
    {
        public static List<CategoryInfo> CategoryColors = new ()
        {
            { new CategoryInfo(Color.Teal, "iOS17Dev", "iOS/iPadOS 17 Developer Beta") },
            { new CategoryInfo(Color.Teal, "iOS17Public", "iOS/iPadOS 17 Public Beta") },
            { new CategoryInfo(Color.Teal, "iOS16Dev", "iOS/iPadOS 16 Developer Beta") },
            { new CategoryInfo(Color.Teal, "iOS16Public", "iOS/iPadOS 16 Public Beta") },
            { new CategoryInfo(Color.Teal, "iOSRetail", "iOS/iPadOS Stable") },
            { new CategoryInfo(Color.Green, "tvOSDev", "tvOS 17 Beta") },
            { new CategoryInfo(Color.Magenta, "macOSDev", "macOS 14 Beta") },
            { new CategoryInfo(Color.Blue, "watchOSDev", "watchOS 10 Beta") },
            { new CategoryInfo(Color.Purple, "audioOSDev", "audioOS 17 Beta") },
        };

        public static List<Device> Devices = new List<Device>()
        {
            Device.IOS17DevBeta,
            Device.IOS17PubBeta,
            Device.IOS16DevBeta,
            Device.IOS16PubBeta,
            Device.IOSReleases,
            Device.TVOS17DevBeta,
            Device.AudiOS17DevBeta,
            Device.MacOSDevBeta,
            Device.WatchOSDevBeta,
        };
        
    }
    
    public sealed class Device
    {
        #region Devices

        public static Device IOS17DevBeta = new Device()
        {
            Audience = "9dcdaf87-801d-42f6-8ec6-307bd2ab9955",
            BuildId = "20D67",
            Product = "iPhone15,3",
            BoardId = "D74AP",
            Name = "iPhone 14 Pro Max",
            Version = "16.3.1",
            FriendlyName = "iOS/iPadOS 17 Dev Beta",
            Category = "iOS17Dev",
            Changelog = "ios",
            Type = ReleaseType.DEVBETA
        };
        public static Device IOS17PubBeta = new Device()
        {
            Audience = "48407998-4446-46b0-9f57-f76b935dc223",
            BuildId = "20D67",
            Product = "iPhone15,3",
            BoardId = "D74AP",
            Name = "iPhone 14 Pro Max",
            Version = "16.3.1",
            Category = "iOS17Public",
            FriendlyName = "iOS/iPadOS 17 Public Beta",
            Changelog = "ios",
            Type = ReleaseType.PUBLICBETA
        };
        public static Device IOS16DevBeta = new Device()
        {
            Audience = "a6050bca-50d8-4e45-adc2-f7333396a42c",
            BuildId = "20D67",
            Product = "iPhone15,3",
            BoardId = "D74AP",
            Name = "iPhone 14 Pro Max",
            Version = "16.3.1",
            FriendlyName = "iOS/iPadOS 16 Dev Beta",
            Changelog = "ios",
            Category = "iOS16Dev",
            Type = ReleaseType.DEVBETA
        };
        public static Device IOS16PubBeta = new Device()
        {
            Audience = "7466521f-cc37-4267-8f46-78033fa700c2",
            BuildId = "20D67",
            Product = "iPhone15,3",
            BoardId = "D74AP",
            Name = "iPhone 14 Pro Max",
            Version = "16.3.1",
            FriendlyName = "iOS/iPadOS 16 Public Beta",
            Changelog = "ios",
            Category = "iOS16Public",
            Type = ReleaseType.DEVBETA
        };
        public static Device IOSReleases = new Device()
        {
            Audience = "01c1d682-6e8f-4908-b724-5501fe3f5e5c",
            BuildId = "20D67",
            Product = "iPhone15,3",
            BoardId = "D74AP",
            Name = "iPhone 14 Pro Max",
            Version = "16.3.1",
            FriendlyName = "iOS/iPadOS Stable",
            Changelog = "ios",
            Category = "iOSRetail",
            Type = ReleaseType.RELEASE
        };
        public static Device TVOS17DevBeta = new Device()
        {
            Audience = "61693fed-ab18-49f3-8983-7c3adf843913",
            BuildId = "17M61",
            Product = "AppleTV6,2",
            BoardId = "J105aAP",
            Name = "Apple TV 4k",
            Version = "13.4.8",
            FriendlyName = "AppleTV 17 Dev Beta",
            Category = "tvOSDev",
            Changelog = "tvos",
            Type = ReleaseType.DEVBETA
        };
        public static Device AudiOS17DevBeta = new Device()
        {
            Audience = "17536d4c-1a9d-4169-bc62-920a3873f7a5",
            BuildId = "20L563",
            Product = "AudioAccessory6,1",
            BoardId = "B620AP",
            Name = "HomePod 2nd Generation",
            Version = "9.9.16.5",
            FriendlyName = "HomePod Beta",
            Category = "audioOSDev",
            Type = ReleaseType.DEVBETA
        };
        public static Device MacOSDevBeta = new Device()
        {
            Audience = "77c3bd36-d384-44e8-b550-05122d7da438",
            BuildId = "22F66",
            Product = "Mac14,3",
            BoardId = "J473AP",
            Name = "M2 Mac Mini",
            Version = "13.4",
            FriendlyName = "Mac OS 14 Dev Beta",
            Category = "macOSDev",
            Changelog = "macos",
            Type = ReleaseType.DEVBETA
        };
        public static Device WatchOSDevBeta = new Device()
        {
            Audience = "7ae7f3b9-886a-437f-9b22-e9f017431b0e",
            BuildId = "20T571",
            Product = "Watch6,15",
            BoardId = "N197bAP",
            Name = "Apple Watch Series 8",
            Version = "9.5.2",
            FriendlyName = "Watch OS Dev Beta",
            Category = "watchOSDev",
            Changelog = "watchos",
            Type = ReleaseType.DEVBETA
        };

        #endregion
        public string Audience { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string BuildId { get; set; }
        public string Product { get; set; }
        public string BoardId { get; set; }
        public string FriendlyName { get; set; }
        public string Category { get; set; }
        public string Changelog { get; set; }
        public ReleaseType Type { get; set; }
    }

    public class CategoryInfo
    {
        public Color Color { get; set; }
        public string CategoryFriendly { get; set; }
        public string Category { get; set; }
        public string Version { get; set; }
        public string ChangeUrl => $"https://developer.apple.com/go/?id={Category}-{Version}-rn";

        public CategoryInfo(Color color, string category, string categoryFriendly)
        {
            Color = color;
            Category = category;
            CategoryFriendly = categoryFriendly;
        }
    }
    public enum ReleaseType
    {
        DEVBETA,
        PUBLICBETA,
        RELEASE
    }
}


/**
https://gist.github.com/Siguza/0331c183c8c59e4850cd0b62fd501424

# AssetAudiences:

# 01c1d682-6e8f-4908-b724-5501fe3f5e5c  iOS release
# ce9c2203-903b-4fb3-9f03-040dc2202694  iOS internal (not publicly accessible)
# 0c88076f-c292-4dad-95e7-304db9d29d34  iOS generic
# c724cb61-e974-42d3-a911-ffd4dce11eda  iOS security updates
# f23050eb-bdfa-4b23-9eca-453e3b1a247c  iOS 11 customer beta
# b7580fda-59d3-43ae-9488-a81b825e3c73  iOS 11 developer beta
# 5839f7cf-9610-483a-980f-6c4266a22f17  iOS 11 public beta
# ef473147-b8e7-4004-988e-0ae20e2532ef  iOS 12 developer beta
# 94bf0742-38e6-4379-adf9-ec9995dde586  iOS 12 public beta
# d8ab8a45-ee39-4229-891e-9d3ca78a87ca  iOS 13 developer beta
# 98bcaac1-66ed-4691-80e4-739f8ed5bb19  iOS 13 public beta
# 84da8706-e267-4554-8207-865ae0c3a120  iOS 14 customer beta
# dbbb0481-d521-4cdf-a2a4-5358affc224b  iOS 14 developer beta
# 1506c359-28af-4ee1-a043-42df9d496d38  iOS 14 public beta
# a98cc469-7f15-4e60-aca5-11a26d60f1e7  iOS 15 customer beta
# ce48f60c-f590-4157-a96f-41179ca08278  iOS 15 developer beta
# 9e12a7a5-36ac-4583-b4fb-484736c739a8  iOS 15 public beta
# 817ce601-f365-4294-8982-b00f547bbe4a  iOS 16 customer beta
# a6050bca-50d8-4e45-adc2-f7333396a42c  iOS 16 developer beta
# 7466521f-cc37-4267-8f46-78033fa700c2  iOS 16 public beta
# 6ce634ea-92a6-4cb9-9610-9c8ba98d67a6  iOS 17 customer beta
# 9dcdaf87-801d-42f6-8ec6-307bd2ab9955  iOS 17 developer beta
# 48407998-4446-46b0-9f57-f76b935dc223  iOS 17 public beta

# 356d9da0-eee4-4c6c-bbe5-99b60eadddf0  tvOS release
# fe6f26f9-ec98-46d2-8faf-565375a83ba7  tvOS generic
# ebd90ea1-6216-4a7c-920e-666faccb2d50  tvOS 11 developer beta (returns 404)
# 5b220c65-fe50-460b-bac5-b6774b2ff475  tvOS 12 developer beta
# b79e95a7-1e51-4a6d-94f8-2bc2f9dbb000  tvOS 13 customer beta
# 975af5cb-019b-42db-9543-20327280f1b2  tvOS 13 developer beta
# a46c2f97-0afb-4a36-bcf6-8c0d74ec21be  tvOS 14 customer beta
# 65254ac3-f331-4c19-8559-cbe22f5bc1a6  tvOS 14 developer beta
# 3565d2d0-06b5-450d-9c01-7086cdd13f97  tvOS 15 customer beta
# 4d0dcdf7-12f2-4ebf-9672-ac4a4459a8bc  tvOS 15 developer beta
# 305f5233-93ed-45a4-9c91-985789b6506b  tvOS 16 customer beta
# d6bac98b-9e2a-4f87-9aba-22c898b25d84  tvOS 16 developer beta
# 0c995cbe-84b5-4ea3-844a-a15a265ac0be  tvOS 16 public beta
# 0e718292-408a-463d-bcc5-8ffc4bdeaabf  tvOS 17 customer beta
# 61693fed-ab18-49f3-8983-7c3adf843913  tvOS 17 developer beta

# b82fcf9c-c284-41c9-8eb2-e69bf5a5269f  watchOS release
# fe4c7f1c-f44c-4c00-b3df-eef225a1ac9d  watchOS generic
# f659e06d-86a2-4bab-bcbb-61b7c60969ce  watchOS 4 developer beta (returns 404)
# e841259b-ad2e-4046-b80f-ca96bc2e17f3  watchOS 5 developer beta
# 7303680f-f711-4020-acbd-58a706de6bf7  watchOS 6 customer beta
# d08cfd47-4a4a-4825-91b5-3353dfff194f  watchOS 6 developer beta
# ff6df985-3cbe-4d54-ba5f-50d02428d2a3  watchOS 7 developer beta
# 6ac47c79-d0c4-42dc-b499-baa45e363c40  watchOS 7 public beta
# b407c130-d8af-42fc-ad7a-171efea5a3d0  watchOS 8 developer beta
# f755ea49-3d47-4829-9cdf-87aa76456282  watchOS 8 public beta
# 2778ab0c-de2e-46b9-83ce-f4b6fd659fa4  watchOS 9 customer beta
# 341f2a17-0024-46cd-968d-b4444ec3699f  watchOS 9 developer beta
# 4935cf61-2a58-437a-be3f-4db423970e43  watchOS 9 public beta
# 982769a5-7551-424f-a599-7a855dddc9e8  watchOS 10 customer beta
# 7ae7f3b9-886a-437f-9b22-e9f017431b0e  watchOS 10 developer beta

# 0322d49d-d558-4ddf-bdff-c0443d0e6fac  audioOS release
# 33c017cc-b820-4b88-8917-6776d7f42b66  audioOS generic
# b05ddb59-b26d-4c89-9d09-5fda15e99207  audioOS 14 customer beta
# 58ff8d56-1d77-4473-ba88-ee1690475e40  audioOS 15 customer beta
# 59377047-7b3f-45b9-8e99-294c0daf3c85  audioOS 16 customer beta
# 3c3d5f0c-1016-426a-9890-11d68820eb13  audioOS 16 public beta
# 17536d4c-1a9d-4169-bc62-920a3873f7a5  audioOS 17 customer beta

# 60b55e25-a8ed-4f45-826c-c1495a4ccc65  macOS release
# 02d8e57e-dd1c-4090-aa50-b4ed2aef0062  macOS generic
# 215447a0-bb03-4e18-8598-7b6b6e7d34fd  macOS 11 customer beta
# ca60afc6-5954-46fd-8cb9-60dde6ac39fd  macOS 11 developer beta
# 902eb66c-8e37-451f-b0f2-ffb3e878560b  macOS 11 public beta
# a3799e8a-246d-4dee-b418-76b4519a15a2  macOS 12 customer beta
# 298e518d-b45e-4d36-94be-34a63d6777ec  macOS 12 developer beta
# 9f86c787-7c59-45a7-a79a-9c164b00f866  macOS 12 public beta
# 3c45c074-41be-4b5b-a511-8592336e6783  macOS 13 customer beta
# 683e9586-8a82-4e5f-b0e7-767541864b8b  macOS 13 developer beta
# 800034a9-994c-4ecc-af4d-7b3b2ee0a5a6  macOS 13 public beta
# 01b45520-b12e-48b3-b30f-46e2795b3eb1  macOS 14 customer beta
# 77c3bd36-d384-44e8-b550-05122d7da438  macOS 14 developer beta
 * 
 */