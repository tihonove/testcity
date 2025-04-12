import * as React from "react";
import { useEffect, useState } from "react";

async function getGravatarUrl(email: string, size: number = 32): Promise<string> {
    const normalizedEmail = email.trim().toLowerCase();
    return `https://www.gravatar.com/avatar/${await getSha256Hash(normalizedEmail)}?s=${size.toString()}&d=identicon`;
}

async function getSha256Hash(message: string): Promise<string> {
    const msgBuffer = new TextEncoder().encode(message);
    const hashBuffer = await crypto.subtle.digest("SHA-256", msgBuffer);
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    const hashHex = hashArray.map(b => b.toString(16).padStart(2, "0")).join("");
    return hashHex;
}

interface GravatarImageProps {
    email: string;
    size?: number;
    alt?: string;
    className?: string;
}

export function GravatarImage({ email, size = 32, alt, className }: GravatarImageProps) {
    const [imageUrl, setImageUrl] = useState<string>("");

    useEffect(() => {
        let isMounted = true;

        const loadGravatarUrl = async () => {
            try {
                const url = await getGravatarUrl(email, size);
                if (isMounted) {
                    setImageUrl(url);
                }
            } catch (error) {
                console.error("Ошибка при загрузке Gravatar:", error);
            }
        };

        void loadGravatarUrl();

        return () => {
            isMounted = false;
        };
    }, [email, size]);

    return <img src={imageUrl} alt={alt || email} className={className} />;
}
