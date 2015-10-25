# CachelessDnsClient
DNS client that directly calls SOA for queries, by passing server caches.  Used for Create operations that tend to populate caches with NXDomain records which prevent clients from connecting for some period of time.
