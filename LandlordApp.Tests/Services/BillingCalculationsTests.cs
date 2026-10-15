using FluentAssertions;
using Lander.src.Modules.Billing.Implementation;
using Xunit;

namespace LandlordApp.Tests.Services;

public class SerbianAccountTests
{
    private static string Valid(string bank, string number)
    {
        var middle = number.PadLeft(13, '0');
        return bank + middle + SerbianAccount.ControlDigits(bank + middle);
    }

    [Fact]
    public void ControlDigits_MakeTheNumberLeaveOneDividedBy97()
    {
        var account = Valid("160", "123456");

        (System.Numerics.BigInteger.Parse(account) % 97).Should().Be(1);
    }

    [Theory]
    [InlineData("{0}-{1}-{2}")]
    [InlineData("{0} {1} {2}")]
    [InlineData(" {0}-{1}-{2} ")]
    public void Normalize_ReadsTheUsualWaysOfWritingIt(string pattern)
    {
        var digits = Valid("160", "123456");
        var written = string.Format(pattern, digits[..3], "123456", digits[16..]);

        SerbianAccount.Normalize(written).Should().Be(digits);
    }

    [Fact]
    public void Normalize_PadsTheMiddlePartToThirteenDigits()
    {
        var digits = Valid("840", "1000");

        SerbianAccount.Normalize($"840-1000-{digits[16..]}").Should().Be(digits);
        SerbianAccount.Normalize(digits).Should().Be(digits);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("160-123456")]
    [InlineData("16-123456-78")]
    [InlineData("160-12345678901234-78")]
    [InlineData("abc-123456-78")]
    [InlineData("1600000001234567")]
    public void Normalize_RefusesWhatIsNotAnAccount(string? input)
    {
        SerbianAccount.Normalize(input).Should().BeNull();
    }

    [Fact]
    public void Normalize_RefusesAMistypedDigit()
    {
        var digits = Valid("160", "123456");
        var mistyped = digits[..10] + (digits[10] == '9' ? '0' : (char)(digits[10] + 1)) + digits[11..];

        SerbianAccount.Normalize(mistyped).Should().BeNull();
    }

    [Fact]
    public void Format_WritesBankNumberAndControl()
    {
        SerbianAccount.Format("160000000012345678").Should().Be("160-0000000123456-78");
    }
}

public class IpsQrTests
{
    private static readonly string Account = "160" + "0000000123456" + SerbianAccount.ControlDigits("1600000000123456");

    private static IpsPayment Payment(decimal amount = 3596.13m, string purpose = "Kirija 10/2026", string? model = "00", string? reference = "12-202610", string? payer = "Ana Anić") =>
        new(Account, "Lazar Lazić", amount, "289", purpose, model, reference, payer);

    [Fact]
    public void Payload_FollowsTheNbsStandard_TagByTagInOrder()
    {
        IpsQr.Payload(Payment()).Should().Be(
            $"K:PR|V:01|C:1|R:{Account}|N:Lazar Lazić|I:RSD3596,13|P:Ana Anić|SF:289|S:Kirija 10/2026|RO:0012-202610");
    }

    [Theory]
    [InlineData(15000, "15000,00")]
    [InlineData(0.5, "0,50")]
    [InlineData(1234567.891, "1234567,89")]
    public void Amount_HasACommaAndTwoDecimals_NoThousandsSeparator(double amount, string expected)
    {
        IpsQr.Amount((decimal)amount).Should().Be(expected);
    }

    [Fact]
    public void Payload_LeavesOutTheReferenceAndPayerWhenThereAreNone()
    {
        var payload = IpsQr.Payload(Payment(model: null, reference: null, payer: null));

        payload.Should().NotContain("|RO:").And.NotContain("|P:");
    }

    [Fact]
    public void Payload_NeverCarriesTheTagSeparatorInsideAValue_AndCutsLongTexts()
    {
        var payload = IpsQr.Payload(Payment(purpose: "Struja | voda i grejanje za ceo oktobar mesec 2026. godine"));

        var purpose = payload.Split('|').Single(p => p.StartsWith("S:"))[2..];
        purpose.Length.Should().BeLessThanOrEqualTo(IpsQr.MaxPurposeLength);
        purpose.Should().StartWith("Struja voda");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1600000000000000ab")]
    public void Payload_RefusesAnAccountThatIsNotEighteenDigits(string account)
    {
        var act = () => IpsQr.Payload(Payment() with { PayeeAccount = account });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Payload_RefusesANonPositiveAmount()
    {
        var act = () => IpsQr.Payload(Payment(amount: 0));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void QrCode_IsAPngDataUrl()
    {
        var url = IpsQr.QrCodeDataUrl(IpsQr.Payload(Payment()));

        url.Should().StartWith("data:image/png;base64,");
        var png = Convert.FromBase64String(url["data:image/png;base64,".Length..]);
        png[..4].Should().Equal(0x89, 0x50, 0x4E, 0x47);
    }
}

public class BillingMathTests
{
    [Theory]
    [InlineData(100.01, 3, new[] { 33.34, 33.34, 33.33 })]
    [InlineData(30001, 2, new[] { 15000.50, 15000.50 })]
    [InlineData(10, 3, new[] { 3.34, 3.33, 3.33 })]
    [InlineData(0.02, 3, new[] { 0.01, 0.01, 0.00 })]
    [InlineData(500, 1, new[] { 500.0 })]
    public void Split_GivesEqualPartsThatAddUpToTheAmount(double amount, int parts, double[] expected)
    {
        var result = BillingMath.Split((decimal)amount, parts);

        result.Should().Equal(expected.Select(e => (decimal)e));
        result.Sum().Should().Be((decimal)amount);
    }

    [Fact]
    public void Split_IntoNoParts_IsAnError()
    {
        var act = () => BillingMath.Split(100, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RentDueDates_ExistInEveryMonth()
    {
        for (var month = 1; month <= 12; month++)
            BillingCalendar.RentDueDate(2027, month, 28).Day.Should().Be(28);
    }
}
