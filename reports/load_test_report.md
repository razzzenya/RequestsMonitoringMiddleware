> test info



test suite: `nbomber_default_test_suite_name`

test name: `nbomber_default_test_name`

session id: `2026-05-27_15-57-47_a62483cf`

> scenario stats



scenario: `allowed_domains`

  - ok count: `524`

  - fail count: `256`

  - all data: `0.133` MB

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
|request count|all = `780`, ok = `524`, RPS = `5.82`|
|latency (ms)|min = `11.98`, mean = `2151.47`, max = `5008.45`, StdDev = `1663.12`|
|latency percentile (ms)|p50 = `1989.63`, p75 = `3794.94`, p95 = `4800.51`, p99 = `4964.35`|
|data transfer (KB)|min = `0.259`, mean = `0.260`, max = `0.263`, all = `0.133` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `780`, fail = `256`, RPS = `2.84`|
|latency (ms)|min = `4999.98`, mean = `5008.26`, max = `5028.24`, StdDev = `4.96`|
|latency percentile (ms)|p50 = `5009.41`, p75 = `5013.5`, p95 = `5017.6`, p99 = `5021.7`|


> status codes for scenario: `allowed_domains`



|status code|count|message|
|---|---|---|
|OK|524||
|-100|256|operation timeout|


> scenario stats



scenario: `greylisted_domains`

  - ok count: `0`

  - fail count: `780`

  - all data: `0.095` MB

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
|request count|all = `780`, ok = `0`, RPS = `0`|
|latency (ms)|min = `0`, mean = `0`, max = `0`, StdDev = `0`|
|latency percentile (ms)|p50 = `0`, p75 = `0`, p95 = `0`, p99 = `0`|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `780`, fail = `780`, RPS = `8.67`|
|latency (ms)|min = `5.79`, mean = `1601.97`, max = `5013.78`, StdDev = `1340.61`|
|latency percentile (ms)|p50 = `1395.71`, p75 = `2347.01`, p95 = `4800.51`, p99 = `5013.5`|
|data transfer (KB)|min = `0.258`, mean = `0.124`, max = `0.258`, all = `0.095` MB|


> status codes for scenario: `greylisted_domains`



|status code|count|message|
|---|---|---|
|-100|26|operation timeout|
|PaymentRequired|376||
|Unauthorized|378|Unexpected status Unauthorized|


> scenario stats



scenario: `unknown_domains`

  - ok count: `0`

  - fail count: `780`

  - all data: `0.186` MB

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
|request count|all = `780`, ok = `0`, RPS = `0`|
|latency (ms)|min = `0`, mean = `0`, max = `0`, StdDev = `0`|
|latency percentile (ms)|p50 = `0`, p75 = `0`, p95 = `0`, p99 = `0`|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `780`, fail = `780`, RPS = `8.67`|
|latency (ms)|min = `6.03`, mean = `1535.15`, max = `5012.93`, StdDev = `1248.9`|
|latency percentile (ms)|p50 = `1405.95`, p75 = `2131.97`, p95 = `4517.89`, p99 = `5009.41`|
|data transfer (KB)|min = `0.25`, mean = `0.243`, max = `0.251`, all = `0.186` MB|


> status codes for scenario: `unknown_domains`



|status code|count|message|
|---|---|---|
|-100|20|operation timeout|
|Unauthorized|760||


