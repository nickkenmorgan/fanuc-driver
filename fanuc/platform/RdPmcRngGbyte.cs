namespace l99.driver.fanuc;

public partial class Platform
{
    
    public async Task<dynamic> RdPmcRngGByteAsync(ushort number)
    {
        return await Task.FromResult(RdPmcRngG(0, 0, number, number, 8 + 1, 0, 0));
    }

    public async Task<dynamic> RdPmcRngYByteAsync(ushort number)
    {
        return await Task.FromResult(RdPmcRngG(2, 0, number, number, 8 + 1, 0, 0));
    }
    

    public async Task<dynamic> RdPmcRngGAsync(short adr_type, short data_type, ushort s_number, ushort e_number,
        ushort length, int IODBPMC_type, int bit)
    {
        return await Task.FromResult(RdPmcRngG(adr_type, data_type, s_number, e_number, length, IODBPMC_type, bit));
    }

    public dynamic RdPmcRngG(short adr_type, short data_type, ushort s_number, ushort e_number, ushort length,
        int IODBPMC_type, int bit)
    {
        dynamic buf = new object();

        switch (IODBPMC_type)
        {
            case 0:
                buf = new Focas.IODBPMC0();
                break;
            case 1:
                buf = new Focas.IODBPMC1();
                break;
            case 2:
                buf = new Focas.IODBPMC2();
                break;
        }

        var ndr = _nativeDispatch(() =>
        {
            return (Focas.focas_ret) Focas.pmc_rdpmcrng(_handle, adr_type, data_type, s_number, e_number, length,
                buf);
        });

        var nr = new
        {
            @null = false,
            method = "pmc_rdpmcrng",
            invocationMs = ndr.ElapsedMilliseconds,
            doc = $"{_docBasePath}/pmc/pmc_rdpmcrng",
            success = ndr.RC == Focas.EW_OK,
            rc = ndr.RC,
            request = new {pmc_rdpmcrng = new {adr_type, data_type, s_number, e_number, length, IODBPMC_type, bit}},
            response = new {pmc_rdpmcrng = new {buf, IODBPMC_type = buf.GetType().Name}}
        };

        _logger.Trace($"[{_machine.Id}] Platform invocation result:\n{JObject.FromObject(nr)}");

        return nr;
    }
}