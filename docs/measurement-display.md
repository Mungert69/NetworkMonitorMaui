# Host measurement display in the MAUI apps

QuantumSecure and NetworkMonitorAgent share this library. Their host details
page and status popup previously displayed encoded aggregates as milliseconds.

The local processor now creates separate UI snapshots in
`MonitorPingProcessor.SetMonitorPingInfoView`, attaching unit, scale and offset
from the host's actual Connect, including dynamic Connects and disabled hosts.
It falls back to the built-in catalogue only when the Connect is absent.
The processor's source readings remain encoded. Copying a MonitorPingInfo to
an MPIndicator now preserves the three metadata fields.

Both apps bind the whole host object through `MeasurementDisplayConverter`:

- Average/minimum/maximum use `MeasurementConversion.Value`.
- Total uses `MeasurementConversion.Total`: scale × stored total + offset ×
  successful reading count. Only duration measurements show that row.
- Missing/failed readings show `Unavailable`; valid negative physical values
  remain valid. Display precision is limited to six decimal places.
- Duration animation speed uses converted milliseconds. Physical readings use
  a neutral animation speed, so current or voltage is not judged as latency.

Single-check responses from ApiService already contain actual milliseconds
(`PhysicalResponseTime`), so their response-time labels require no extra scaling.
No API/processor message fields or database migrations are added by this fix.

## Checks

Run `dotnet test Tests/NetworkMonitorMaui.Tests/NetworkMonitorMaui.Tests.csproj`.
The converter tests cover voltage, negative current, durations above 65,535ms,
failure markers, empty datasets, offset-aware totals and metadata copies.
The test dependencies match the referenced shared library's test SDK, Moq,
logging and dependency-injection versions to avoid restore downgrade errors.

On Google Play internal testing, check both apps:

1. Normal HTTP/ICMP hosts show milliseconds in popup and details.
2. A scaled long-duration host shows its actual milliseconds, including min/max.
3. A Victron voltage sample encoded as 34073 at scale 0.01/offset -327.68
   shows `13.05 V`. A current sample 32757 at scale 0.1/offset -3276.8
   shows `-1.1 A`.
4. A host with no successful samples shows `Unavailable`, never an offset-derived
   reading. Test a failed sample as well.
5. Physical hosts do not show a total-duration row and use neutral animation speed.
6. Switching selected hosts and receiving new readings updates the details.
7. Confirm existing alert flags and packet-loss indicators still work.

Updated repositories: QuantumSecure, NetworkMonitorAgent, NetworkMonitorMaui,
NetworkMonitorLib and NetworkMonitorProcessorAgent. Build/deploy the apps using
all matching shared sources. Android builds are intentionally left for the
user's build environment; static XML checks can verify the changed XAML syntax
but cannot replace device testing.
