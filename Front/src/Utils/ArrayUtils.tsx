export function stableGroupBy<T, K>(items: T[], keyFn: (item: T) => K): Map<K, T[]> {
    const result = new Map<K, T[]>();

    for (const item of items) {
        const key = keyFn(item);
        let group = result.get(key);
        if (group == undefined) {
            group = [];
            result.set(key, group);
        }
        group.push(item);
    }

    return result;
}
