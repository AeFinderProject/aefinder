using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AElfScan.AElf.Etos;

[EventName("AElf.ConfirmLogEvents")]
public class ConfirmLogEventsEto
{
    public List<ConfirmLogEventEto> ConfirmLogEvents { get; set; }
}