export function reject(message: string): never {
    throw new Error(message);
}

export function runAsyncAction(action: () => Promise<void>) {
    // eslint-disable-next-line @typescript-eslint/no-floating-promises
    action();
}
