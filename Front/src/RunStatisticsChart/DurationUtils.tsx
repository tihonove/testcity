export function formatDuration(maxDuration: number, duration: number): string {
    if (maxDuration <= 1000) {
        return `${duration} ms`;
    }
    if (maxDuration <= 60 * 1000) {
        return `${duration / 1000} s`;
    }
    if (maxDuration <= 20 * 60 * 1000) {
        return `${Math.floor(duration / (1000 * 60))}:${Math.ceil(Math.floor(duration % (1000 * 60)) / 1000).toString().padStart(2, '0')} m`;
    }
    if (maxDuration <= 60 * 60 * 1000) {
        return `${duration / 1000 / 60} m`;
    }
    return `${Math.floor(duration / (1000 * 60 * 60))}:${Math.ceil(Math.floor(duration % (1000 * 60 * 60)) / (1000 * 60)).toString().padStart(2, '0')} h`;
}