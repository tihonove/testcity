export function reject(message: string): never {
    throw new Error(message);
}