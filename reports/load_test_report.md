> test info



test suite: `nbomber_default_test_suite_name`

test name: `nbomber_default_test_name`

session id: `2026-06-14_12-17-51_657724ff`

> scenario stats



scenario: `allowed_domains`

  - ok count: `780`

  - fail count: `0`

  - all data: `0.199` MB

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
|request count|all = `780`, ok = `780`, RPS = `8.67`|
|latency (ms)|min = `6.9`, mean = `271.46`, max = `4536.74`, StdDev = `418.35`|
|latency percentile (ms)|p50 = `153.09`, p75 = `371.71`, p95 = `835.58`, p99 = `1213.44`|
|data transfer (KB)|min = `0.259`, mean = `0.261`, max = `0.263`, all = `0.199` MB|


> status codes for scenario: `allowed_domains`



|status code|count|message|
|---|---|---|
|OK|780||


> scenario stats



scenario: `greylisted_domains`

  - ok count: `780`

  - fail count: `0`

  - all data: `0.196` MB

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
|request count|all = `780`, ok = `780`, RPS = `8.67`|
|latency (ms)|min = `3.77`, mean = `162.76`, max = `4459.21`, StdDev = `306.5`|
|latency percentile (ms)|p50 = `66.62`, p75 = `208.26`, p95 = `578.56`, p99 = `965.63`|
|data transfer (KB)|min = `0.257`, mean = `0.257`, max = `0.258`, all = `0.196` MB|


> status codes for scenario: `greylisted_domains`



|status code|count|message|
|---|---|---|
|PaymentRequired|780||


> scenario stats



scenario: `unknown_domains`

  - ok count: `780`

  - fail count: `0`

  - all data: `0.189` MB

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
|request count|all = `780`, ok = `780`, RPS = `8.67`|
|latency (ms)|min = `3.63`, mean = `175.5`, max = `4476.36`, StdDev = `331.78`|
|latency percentile (ms)|p50 = `65.09`, p75 = `208.64`, p95 = `701.44`, p99 = `977.92`|
|data transfer (KB)|min = `0.247`, mean = `0.247`, max = `0.248`, all = `0.189` MB|


> status codes for scenario: `unknown_domains`



|status code|count|message|
|---|---|---|
|Forbidden|780||


