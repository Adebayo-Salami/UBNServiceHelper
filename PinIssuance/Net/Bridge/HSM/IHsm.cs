using System.Runtime.InteropServices;

namespace PinIssuance.Net.Bridge.HSM
{
    public interface IHsm
    {
        void Setup(string hostname,
            int port,
            [Optional]
            int? headerLength,
            [Optional]
            int? encryptedPinLength);
        IZonePinEncryptionKey ZonePinKeyManager();
        IPinTranslation PinTranslator();
        IPinGeneration PinGenerator();
        IPinVerification PinVerifier();
        void Close();
    }
    public interface IGenerateCVVResponse
    {
        string Cvv { get; set; }
    }
    public interface ICvvGeneration
    {
        IGenerateCVVResponse GenerateCvv(
            string pan, string expiryDate);

        IGenerateCVVResponse GenerateCvv(
            string pan,
            string expiryDate,
            string cardVerificationKey,
            string serviceCode);

    }
    public interface IZonePinEncryptionKey
    {
        IGeneratePinEncryptionKeyResponse Generate(
            string exchangeKey,
            [Optional]
            KeyScheme? exchangeKeyScheme,
            [Optional]
            KeyScheme? storageKeyScheme
            );

        ITranslatePinEncryptionKeyResponse TranslateFromExchangeKeyToStorageKey(
            string exchangeKey,
            string pinEncryptionKey,
            [Optional]
            KeyScheme? exchangeKeyScheme,
            [Optional]
            KeyScheme? storageKeyScheme
            );

        ITranslatePinEncryptionKeyResponse TranslateFromStorageKeyToExchangeKey(
            string exchangeKey,
            string pinEncryptionKey,
            [Optional]
            KeyScheme? exchangeKeyScheme,
            [Optional]
            KeyScheme? storageKeyScheme
            );
    }

    public interface IPinTranslation
    {
        IPinTranslationResponse TranslateFromPinEncryptionKeyToAnother(
            bool isInterchange,
            string sourcePinEncryptionKey,
            string destinationPinEncryptionKey,
            int maxPinLength,
            string pinBlock,
            PinBlockFormats sourcePinBlockFormat,
            PinBlockFormats destinationPinBlockFormat,
            string accountNumber);

        IPinGenerationResponse TranslatePinFromBdkToZpkEncryption(
            string bdk,
            string zpk,
            string keySerialNumber,
            string pinBlock,
            string accountNumber);


        IPinGenerationResponse TranslateFromPinEncryptionKeyToStorageKey(
            bool isInterchange,
            string pinEncryptionKey,
            string pinBlock,
            PinBlockFormats pinBlockFormat,
            string accountNumber);
    }

    public interface IPinGeneration
    {
        IPinGenerationResponse GenerateRandomPin(
           string accountNumber,
           [Optional]
           int? pinLength);

        IPinGenerationResponse EncryptClearPin(
           string clearPin,
           string accountNumber);

        IGeneratePinOffsetResponse GeneratePinOffset(
            string encryptedPin,
            string accountNumber);

        IGeneratePinOffsetResponse GeneratePinOffset(
            string encryptedPin,
            string accountNumber,
            string cardPan);

        IGeneratePinOffsetResponse GenerateVISAPinOffset(
            string encryptedPIN, string accountNo, string CardPAN);

        IGeneratePinOffsetResponse GeneratePVV(
            string encryptedPin, string accountNumber, string cardPan);

        IGeneratePinOffsetResponse GeneratePinOffsetFromPinBlock(
            string encryptedPinBlock,
            string accountNumber,
            PinBlockFormats encryptedPinBlockFormat);

        IPinGenerationResponse DeriveEncryptedPin(
            string accountNumber);
    }

    public interface IPinVerification
    {
        void VerifyPIN(
            bool isInterchange,
            string pinEncryptionKey,
            string pinVerificationKey,
            string encryptedPinBlock,
            PinBlockFormats encryptedPinBlockFormat,
            int minPinLength,
            string accountNumber,
            string pinValidationData,
            string offset);
    }

    #region Enums 
    public enum PinBlockFormats
    {
        ANSI = 1,
        DOCUTEL = 2,
        DIEBOLD = 3,
        PLUS = 4,
        ISO9564 = 5
    }

    public enum KeyScheme
    {
        Single_ANSI = 'Z',
        Double_ANSI = 'X',
        Triple_ANSI = 'Y',
        Double_Variant = 'U',
        Triple_Variant = 'T'
    }
    #endregion

    #region Responses

    public interface IGeneratePinEncryptionKeyResponse
    {
        string PinEncryptionKeyUnderExchangeKey { get; set; }
        string PinEncryptionKeyUnderStorageKey { get; set; }
        string CheckValue { get; set; }
    }

    public interface ITranslatePinEncryptionKeyResponse
    {
        string TranslatedPinEncryptionKey { get; set; }
        string KeyCheckValue { get; set; }
    }

    public interface IPinTranslationResponse
    {
        int PinLength { get; set; }
        string PinBlock { get; set; }
        PinBlockFormats PinBlockFormat { get; set; }
    }

    public interface IPinGenerationResponse
    {
        string EncryptedPin { get; set; }
    }

    public interface IGeneratePinOffsetResponse
    {
        string Offset { get; set; }
    }


    #endregion

}
