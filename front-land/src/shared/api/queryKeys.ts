// Query keys that more than one place has to know: whoever changes the data invalidates it, whoever shows it
// reads it, and a notification that arrives live refreshes it.
export const MY_PAIRING_KEY = 'my-pairing';
export const SHARED_APARTMENTS_KEY = 'shared-apartments';
// Bills: the header badge reads the summary; the bills pages read the rest. A bill notification refreshes all.
export const BILLS_KEY = 'bills';
export const BILLS_SUMMARY_KEY = 'bills-summary';
export const TENANCIES_KEY = 'tenancies';
