import { Button, Input } from "@skbkontur/react-ui";
import * as React from "react";
import { useApiUrl } from "../Domain/Navigation";
import { runAsyncAction } from "../Utils/TypeHelpers";

export function AppNewProjectsWizard() {
    const apiUrl = useApiUrl();
    const [projectId, setProjectId] = React.useState<string>("");
    const [accessChecked, setAccessChecked] = React.useState<undefined | boolean>(undefined);
    const [webhookIsReady, setWebhookIsReady] = React.useState<undefined | boolean>(undefined);
    const [accessCheckResponseMessage, setAccessCheckResponseMessage] = React.useState<string | null>(null);
    const [checkingAccess, setCheckingAccess] = React.useState<boolean>(false);

    const handleCheckAccess = () => {
        runAsyncAction(async () => {
            // Сбрасываем предыдущие результаты
            setAccessCheckResponseMessage(null);

            if (!projectId || isNaN(Number(projectId))) {
                setAccessCheckResponseMessage("Пожалуйста, введите корректный ID проекта");
                setAccessChecked(false);
                return;
            }

            setCheckingAccess(true);
            try {
                const response = await fetch(`${apiUrl}projects/gitlab/${projectId}/access-check`);

                if (response.ok) {
                    // Доступ предоставлен
                    setAccessChecked(true);
                } else {
                    // Доступ не предоставлен
                    const errorText = await response.text();
                    setAccessCheckResponseMessage(errorText);
                    setAccessChecked(false);
                }
            } catch (error) {
                console.error("Ошибка при проверке доступа:", error);
                setAccessCheckResponseMessage("Произошла ошибка при проверке доступа к проекту");
                setAccessChecked(false);
            } finally {
                setCheckingAccess(false);
            }
        });
    };

    const handleCheckWebhook = () => {
        // Реализация проверки веб-хука будет добавлена позже
        setWebhookIsReady(true); // Временная заглушка
    };

    const handleAddProject = () => {
        // Реализация проверки веб-хука будет добавлена позже
    };

    return (
        <section>
            <h1>New Projects Wizard</h1>
            <h2>Step 1</h2>
            Check access
            <Input value={projectId} onValueChange={setProjectId} />
            <Button onClick={handleCheckAccess} disabled={checkingAccess} loading={checkingAccess}>
                Check
            </Button>
            {accessChecked === true && <div style={{ color: "green" }}>Access granted</div>}
            {accessChecked === false && (
                <div>
                    <div style={{ color: "red" }}>Access not granted</div>
                    {accessCheckResponseMessage && <div style={{ color: "red" }}>{accessCheckResponseMessage}</div>}
                </div>
            )}
            <h2>Step 2</h2>
            Add webhook Instruction
            <Button onClick={handleCheckWebhook}>Check webhook</Button>
            {webhookIsReady === true && <div style={{ color: "green" }}>Ok</div>}
            {webhookIsReady === false && <div style={{ color: "red" }}>Not ok</div>}
            <h2>Step 3</h2>
            Enable project
            <Button onClick={handleAddProject} disabled={!accessChecked || !webhookIsReady}>
                Add project
            </Button>
        </section>
    );
}
