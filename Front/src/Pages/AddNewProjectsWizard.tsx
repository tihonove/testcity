import { Button, Input } from "@skbkontur/react-ui";
import * as React from "react";
import { useApiUrl } from "../Domain/Navigation";
import { runAsyncAction } from "../Utils/TypeHelpers";
import styles from "./AddNewProjectsWizard.module.css";
import getProjectIdImage from "./Images/GetProjectId.png";
import addWebHookButton from "./Images/AddWebHookButton.png";
import hookSettings from "./Images/HookSettings.png";
import { LogoPageBlock } from "../Components/LogoPageBlock";

export function AddNewProjectsWizard() {
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
                setAccessCheckResponseMessage("Please enter a valid project ID");
                setAccessChecked(false);
                return;
            }

            setCheckingAccess(true);
            try {
                const response = await fetch(`${apiUrl}gitlab/projects/${projectId}/access-check`);

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
                setAccessCheckResponseMessage("An error occurred while checking access to the project");
                setAccessChecked(false);
            } finally {
                setCheckingAccess(false);
            }
        });
    };

    const handleAddProject = () => {
        runAsyncAction(async () => {
            if (!projectId || !accessChecked || !webhookIsReady) {
                return;
            }

            try {
                const response = await fetch(`${apiUrl}gitlab/projects/${projectId}/add`, {
                    method: "POST",
                });

                if (response.ok) {
                    setAccessCheckResponseMessage("Project successfully added");
                } else {
                    const errorText = await response.text();
                    setAccessCheckResponseMessage(`Error adding project: ${errorText}`);
                }
            } catch (error) {
                setAccessCheckResponseMessage("An error occurred while adding the project");
            }
        });
    };

    const handleCheckWebhook = () => {
        setWebhookIsReady(true);
    };

    return (
        <div className={styles.root}>
            <LogoPageBlock />
            <main className={styles.content}>
                <h1>New project wizard</h1>
                <h2>Step 1</h2>
                <p>Get project id and check access</p>
                <p>
                    If the project is private, you need to grant access to the project for the system account{" "}
                    <a href="https://git.skbkontur.ru/svc_testcity_gitlab">svc_testcity_gitlab</a>.
                </p>
                <img src={getProjectIdImage} />
                <p>Enter the project ID in the field below and click "Check"</p>
                <p>
                    <Input
                        width={300}
                        value={projectId}
                        onValueChange={setProjectId}
                        placeholder="Project id, for example 20456"
                    />
                </p>
                <Button
                    use={accessChecked ? "success" : "primary"}
                    onClick={handleCheckAccess}
                    disabled={checkingAccess}
                    loading={checkingAccess}>
                    {accessChecked ? "Access granted" : "Check"}
                </Button>
                {accessChecked === false && (
                    <div>
                        <div style={{ color: "red" }}>Access not granted</div>
                        {accessCheckResponseMessage && <div style={{ color: "red" }}>{accessCheckResponseMessage}</div>}
                    </div>
                )}
                <h2>Step 2</h2>
                <p>
                    Add a webhook to your project. Navigate to Settings &gt; Webhooks and click the "Add webhook" button
                </p>
                <img src={addWebHookButton} />
                <p>Use the following URL for your webhook: </p>
                <b>{`${window.location.protocol}//${window.location.host}/api/gitlab/webhook`}</b>
                <p>Configure with these settings: Trigger: Push events: All branches, Events: Job events</p>
                <img src={hookSettings} />
                <Button onClick={handleCheckWebhook} use={webhookIsReady ? "success" : "primary"}>
                    I've added webhook
                </Button>
                <h2>Step 3</h2>
                <p>Add project for synchronization</p>
                <Button onClick={handleAddProject} disabled={!accessChecked || !webhookIsReady}>
                    Add project
                </Button>
            </main>
        </div>
    );
}
