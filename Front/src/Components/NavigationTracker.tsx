import { useEffect, useRef } from "react";
import { useLocation } from "react-router-dom";
import { useApiUrl } from "../Domain/Navigation";
import { getOrCreateUserId } from "../Utils/UserIdentification";

export function NavigationTracker(): null {
    const location = useLocation();
    const apiUrl = useApiUrl();
    const lastTrackedRouteRef = useRef<string | null>(null);

    useEffect(() => {
        const currentRoute = location.pathname;
        if (lastTrackedRouteRef.current === currentRoute) {
            return;
        }        
        lastTrackedRouteRef.current = currentRoute;
        const userId = getOrCreateUserId();        
        fetch(`${apiUrl}track/route`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                userId,
                route: currentRoute
            }),
        });
    }, [location.pathname, apiUrl]);

    return null;
}