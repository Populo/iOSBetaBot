namespace iOSBot.Service
{
    public class AssetResponse
    {
        public string ArchiveID { get; set; }
        public string _EventRecordingServiceURL { get; set; }
        public bool SUInstallTonightEnabled { get; set; }
        public string InstallationSizeSnapshot { get; set; }
        public long _UnarchivedSize { get; set; }
        public string __AssetDefaultGarbageCollectionBehavior { get; set; }
        public string OSVersion { get; set; }
        public string[] SupportedDeviceModels { get; set; }
        public string _Measurement { get; set; }
        public string InstallationSize { get; set; }
        public string[] SupportedDevices { get; set; }
        public string _MeasurementAlgorithm { get; set; }
        public bool SUMultiPassEnabled { get; set; }
        public string SEPTBMDigests { get; set; }
        public string __BaseURL { get; set; }
        public string AssetType { get; set; }
        public long _DownloadSize { get; set; }
        public bool SUConvReqd { get; set; }
        public bool __CanUseLocalCacheServer { get; set; }
        public string __RelativePath { get; set; }
        public bool SUDisableNonsnapshotUpdate { get; set; }
        public string Build { get; set; }
        public string SEPDigest { get; set; }
        public string SUProductSystemName { get; set; }
        public _Assetreceipt _AssetReceipt { get; set; }
        public string SUDocumentationID { get; set; }
        public string _CompressionAlgorithm { get; set; }
        public string RSEPDigest { get; set; }
        public Cryptexsizeinfo[] CryptexSizeInfo { get; set; }
        public string RSEPTBMDigests { get; set; }
        public string SUPublisher { get; set; }
        public int SystemVolumeSealingOverhead { get; set; }
        public bool _IsZipStreamable { get; set; }
        public Systempartitionpadding SystemPartitionPadding { get; set; }
        public int ActualMinimumSystemPartition { get; set; }
        public int MinimumSystemPartition { get; set; }
        public bool Ramp { get; set; }
    }

    public class _Assetreceipt
    {
        public string AssetReceipt { get; set; }
        public string AssetSignature { get; set; }
    }

    public class Systempartitionpadding
    {
        public int _512 { get; set; }
        public int _16 { get; set; }
        public int _768 { get; set; }
        public int _1024 { get; set; }
        public int _128 { get; set; }
        public int _32 { get; set; }
        public int _8 { get; set; }
        public int _64 { get; set; }
        public int _256 { get; set; }
    }

    public class Cryptexsizeinfo
    {
        public string CryptexTag { get; set; }
        public int CryptexSize { get; set; }
    }
}