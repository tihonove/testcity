export function formatDuration(maxDuration: number, duration: number): string {
    if (maxDuration <= 1000) {
        return `${duration.toFixed(2)} ms`;
    }
    if (maxDuration <= 60 * 1000) {
        return `${(duration / 1000).toFixed(2)} s`;
    }
    if (maxDuration <= 20 * 60 * 1000) {
        return `${Math.floor(duration / (1000 * 60)).toString()}:${Math.ceil(Math.floor(duration % (1000 * 60)) / 1000)
            .toString()
            .padStart(2, "0")} m`;
    }
    if (maxDuration <= 60 * 60 * 1000) {
        return `${(duration / 1000 / 60).toFixed(2)} m`;
    }
    return `${Math.floor(duration / (1000 * 60 * 60)).toString()}:${Math.ceil(
        Math.floor(duration % (1000 * 60 * 60)) / (1000 * 60)
    )
        .toString()
        .padStart(2, "0")} h`;
}
