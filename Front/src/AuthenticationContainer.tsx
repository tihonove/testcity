import * as React from "react";
import { useLocalStorage } from "usehooks-ts";
import { useTestCityClient } from "./TestCityApiClient";
import { runAsyncAction } from "./Utils/TypeHelpers";
import { UserProvider, UserInfo } from "./Contexts/UserContext";

interface AuthenticationContainerProps {
    children: React.ReactNode;
}

interface AuthAttempts {
    count: number;
    timestamp: number;
}

export function AuthenticationContainer({ children }: AuthenticationContainerProps) {
    const api = useTestCityClient();
    const [user, setUser] = React.useState<UserInfo | null>(null);
    const [isLoading, setIsLoading] = React.useState(true);
    const [authError, setAuthError] = React.useState<string | null>(null);
    const [hasInitialized, setHasInitialized] = React.useState(false);

    const MAX_AUTH_ATTEMPTS = 3;
    const ATTEMPT_RESET_TIME = 5 * 60 * 1000;

    const [authAttempts, setAuthAttempts] = useLocalStorage<AuthAttempts>("auth_redirect_attempts", {
        count: 0,
        timestamp: Date.now(),
    });

    const checkAndResetAttempts = React.useCallback(() => {
        const now = Date.now();
        if (now - authAttempts.timestamp > ATTEMPT_RESET_TIME) {
            setAuthAttempts({ count: 0, timestamp: now });
            return { count: 0, timestamp: now };
        }
        return authAttempts;
    }, [authAttempts, setAuthAttempts]);

    const incrementAuthAttempts = React.useCallback(() => {
        const currentAttempts = checkAndResetAttempts();
        const newAttempts = {
            count: currentAttempts.count + 1,
            timestamp: Date.now(),
        };
        setAuthAttempts(newAttempts);
        return newAttempts.count;
    }, [checkAndResetAttempts, setAuthAttempts]);

    const clearAuthAttempts = React.useCallback(() => {
        setAuthAttempts({ count: 0, timestamp: Date.now() });
    }, [setAuthAttempts]);

    const handleRedirectToLogin = React.useCallback(() => {
        const attempts = incrementAuthAttempts();

        if (attempts > MAX_AUTH_ATTEMPTS) {
            setAuthError(
                `Превышено максимальное количество попыток входа (${MAX_AUTH_ATTEMPTS.toString()}). Попробуйте обновить страницу через несколько минут.`
            );
            return;
        }

        window.location.href = "/api/auth/login?returnUrl=" + encodeURIComponent(window.location.href);
    }, [incrementAuthAttempts]);

    React.useEffect(() => {
        if (hasInitialized) return;

        runAsyncAction(async () => {
            try {
                setIsLoading(true);
                const userResponse = await api.getUser();
                if (userResponse == null) {
                    handleRedirectToLogin();
                } else {
                    setUser(userResponse);
                    // Очищаем счетчик попыток при успешной аутентификации
                    clearAuthAttempts();
                }
            } catch (error) {
                console.error("Ошибка при получении информации о пользователе:", error);
                handleRedirectToLogin();
            } finally {
                setIsLoading(false);
                setHasInitialized(true);
            }
        });
    }, []);

    if (isLoading) {
        return <div>Loading...</div>;
    }

    if (authError) {
        return (
            <div style={{ padding: "20px", textAlign: "center" }}>
                <h2>Ошибка аутентификации</h2>
                <p>{authError}</p>
                <button
                    onClick={() => {
                        clearAuthAttempts();
                        window.location.reload();
                    }}
                    style={{
                        marginTop: "10px",
                        padding: "10px 20px",
                        backgroundColor: "#007acc",
                        color: "white",
                        border: "none",
                        borderRadius: "4px",
                        cursor: "pointer",
                    }}>
                    Попробовать снова
                </button>
            </div>
        );
    }

    if (!user) {
        return <></>;
    }

    return <UserProvider user={user}>{children}</UserProvider>;
}
