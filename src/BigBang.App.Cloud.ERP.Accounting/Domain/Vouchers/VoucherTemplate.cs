namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers
{
    public enum VoucherTemplate : byte
    {
        Custom = 0,
        Expenses = 1,
        ProductBuy = 2,
        ProductSell = 3,
        Cost = 4,
        PayDebt = 5,
        Deposit = 6,
        ReceiveDebt = 7,
        AdvanceReceipt = 8,
        AdvancePayment = 9
    }
}