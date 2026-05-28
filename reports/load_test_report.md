> test info



test suite: `nbomber_default_test_suite_name`

test name: `nbomber_default_test_name`

session id: `2026-05-28_14-54-55_a19b202d`

> scenario stats



scenario: `allowed_domains`

  - ok count: `715`

  - fail count: `65`

  - all data: `0.182` MB

  - duration: `00:01:30`

load simulations:

  - `ramping_inject`, rate: `5`, interval: `00:00:01`, during: `00:00:15`

  - `inject`, rate: `5`, interval: `00:00:01`, during: `00:00:15`

  - `ramping_inject`, rate: `10`, interval: `00:00:01`, during: `00:00:15`

  - `inject`, rate: `10`, interval: `00:00:01`, during: `00:00:15`

  - `ramping_inject`, rate: `15`, interval: `00:00:01`, during: `00:00:15`

  - `inject`, rate: `15`, interval: `00:00:01`, during: `00:00:15`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `780`, ok = `715`, RPS = `7.94`|
|latency (ms)|min = `11.94`, mean = `1189.36`, max = `4989.67`, StdDev = `1316.95`|
|latency percentile (ms)|p50 = `628.74`, p75 = `1709.06`, p95 = `4276.22`, p99 = `4902.91`|
|data transfer (KB)|min = `0.259`, mean = `0.261`, max = `0.263`, all = `0.182` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `780`, fail = `65`, RPS = `0.72`|
|latency (ms)|min = `5000.91`, mean = `5009.36`, max = `5016.65`, StdDev = `5.04`|
|latency percentile (ms)|p50 = `5013.5`, p75 = `5017.6`, p95 = `5017.6`, p99 = `5017.6`|


> status codes for scenario: `allowed_domains`



|status code|count|message|
|---|---|---|
|OK|715||
|-100|65|operation timeout|


> scenario stats



scenario: `greylisted_domains`

  - ok count: `757`

  - fail count: `23`

  - all data: `0.190` MB

  - duration: `00:01:30`

load simulations:

  - `ramping_inject`, rate: `5`, interval: `00:00:01`, during: `00:00:15`

  - `inject`, rate: `5`, interval: `00:00:01`, during: `00:00:15`

  - `ramping_inject`, rate: `10`, interval: `00:00:01`, during: `00:00:15`

  - `inject`, rate: `10`, interval: `00:00:01`, during: `00:00:15`

  - `ramping_inject`, rate: `15`, interval: `00:00:01`, during: `00:00:15`

  - `inject`, rate: `15`, interval: `00:00:01`, during: `00:00:15`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `780`, ok = `757`, RPS = `8.41`|
|latency (ms)|min = `5.73`, mean = `736.69`, max = `4845.51`, StdDev = `946.85`|
|latency percentile (ms)|p50 = `318.72`, p75 = `961.02`, p95 = `2818.05`, p99 = `4120.58`|
|data transfer (KB)|min = `0.257`, mean = `0.257`, max = `0.258`, all = `0.190` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `780`, fail = `23`, RPS = `0.26`|
|latency (ms)|min = `5000.42`, mean = `5008.6`, max = `5015.91`, StdDev = `4.05`|
|latency percentile (ms)|p50 = `5009.41`, p75 = `5013.5`, p95 = `5013.5`, p99 = `5017.6`|


> status codes for scenario: `greylisted_domains`



|status code|count|message|
|---|---|---|
|PaymentRequired|757||
|-100|23|operation timeout|


> scenario stats



scenario: `unknown_domains`

  - ok count: `768`

  - fail count: `12`

  - all data: `0.188` MB

  - duration: `00:01:30`

load simulations:

  - `ramping_inject`, rate: `5`, interval: `00:00:01`, during: `00:00:15`

  - `inject`, rate: `5`, interval: `00:00:01`, during: `00:00:15`

  - `ramping_inject`, rate: `10`, interval: `00:00:01`, during: `00:00:15`

  - `inject`, rate: `10`, interval: `00:00:01`, during: `00:00:15`

  - `ramping_inject`, rate: `15`, interval: `00:00:01`, during: `00:00:15`

  - `inject`, rate: `15`, interval: `00:00:01`, during: `00:00:15`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `780`, ok = `768`, RPS = `8.53`|
|latency (ms)|min = `5.84`, mean = `757.03`, max = `4881.34`, StdDev = `973.42`|
|latency percentile (ms)|p50 = `327.42`, p75 = `976.38`, p95 = `2867.2`, p99 = `4198.4`|
|data transfer (KB)|min = `0.25`, mean = `0.25`, max = `0.251`, all = `0.188` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `780`, fail = `12`, RPS = `0.13`|
|latency (ms)|min = `5001.97`, mean = `5007.75`, max = `5013.55`, StdDev = `4.19`|
|latency percentile (ms)|p50 = `5009.41`, p75 = `5013.5`, p95 = `5013.5`, p99 = `5017.6`|


> status codes for scenario: `unknown_domains`



|status code|count|message|
|---|---|---|
|Unauthorized|768||
|-100|12|operation timeout|


