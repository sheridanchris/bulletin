[<AutoOpen>]
module Logger

open FsLibLog
let rec logger = LogProvider.getLoggerByQuotation <@ logger @>
