export function uuidv4() {
    if (typeof crypto != "undefined") {
        return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c => (+c ^ (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (+c / 4)))).toString(16)
        );
    } else {
        const w = () => {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        };
        return `${w()}${w()}-${w()}-${w()}-${w()}-${w()}${w()}${w()}`;
    }
}
