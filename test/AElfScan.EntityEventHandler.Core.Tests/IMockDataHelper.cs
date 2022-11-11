using AElfScan.Etos;

namespace AElfScan.EntityEventHandler.Core.Tests.AElf;

public interface IMockDataHelper
{
    string CreateBlockHash();

    NewBlockEto MockNewBlockEtoData(long blockNumber, string previousBlockHash);

    NewBlockEto MockNewBlockEtoData(string blockHash, long blockNumber, string previousBlockHash);

    ConfirmBlockEto MockConfirmBlockEtoData(NewBlockEto newBlockEto);
    
    ConfirmBlockEto MockConfirmBlockEtoData(string currentBlockHash, long blockNumber);
}