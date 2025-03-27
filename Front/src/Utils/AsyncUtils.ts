export function delay(tm: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, tm));
}
