import { Button, Modal } from "@skbkontur/react-ui";
import * as React from "react";
import styles from "./AddNewProjectModal.module.css";

interface AddNewProjectModalProps {
    onClose: () => void;
}

export function AddNewProjectModal(props: AddNewProjectModalProps) {
    return (
        <Modal width="700px" onClose={props.onClose} ignoreBackgroundClick>
            <Modal.Header>Добавить новый проект</Modal.Header>
            <Modal.Body>
                <p className={styles.paragraph}>Добавление нового проекта на текущем этапе происходит вручную.</p>
                <p className={styles.paragraph}>
                    Чтобы добавить проект, обратитесь в канал{" "}
                    <a target="_blank" href="https://chat.skbkontur.ru/kontur/channels/testcity">
                        #testcity
                    </a>{" "}
                    в Mattermost или в личку к{" "}
                    <a target="_blank" href="https://staff.skbkontur.ru/profile/tihonove">
                        @tihonove
                    </a>
                    .{" "}
                </p>
                <p className={styles.header4}>Важная информация:</p>
                <p className={styles.paragraph}>
                    Чтобы тесты были видны за пределами GitLab, надо, чтобы тесты попадали в артефакты, а не только в
                    JUnit report. (Подробнее см.{" "}
                    <a
                        target="_blank"
                        href="https://docs.gitlab.com/api/job_artifacts/#downloading-artifactsreports-files">
                        документацию GitLab
                    </a>
                    .)
                </p>
                <p className={styles.paragraph}>
                    Если ваш проект не открыт для всех контуровцев, необходимо будет выдать доступ учетной записи, под
                    которой работает сервис (уточним при добавлении).
                </p>
            </Modal.Body>
            <Modal.Footer panel>
                <Button onClick={props.onClose}>Закрыть</Button>
            </Modal.Footer>
        </Modal>
    );
}
