import { Button, Modal } from "@skbkontur/react-ui";
import * as React from "react";
import styled from "styled-components";

interface AddNewProjectModalProps {
    onClose: () => void;
}

export function AddNewProjectModal(props: AddNewProjectModalProps) {
    return (
        <Modal width="700px" onClose={props.onClose} ignoreBackgroundClick>
            <Modal.Header>Добавить новый проект</Modal.Header>
            <Modal.Body>
                <Paragraph>Добавление нового проекта на текущем этапе происходит вручную.</Paragraph>
                <Paragraph>
                    Чтобы добавить проект, обратитесь в канал{" "}
                    <a target="_blank" href="https://chat.skbkontur.ru/kontur/channels/testcity">
                        #testcity
                    </a>{" "}
                    в Mattermost или в личку к{" "}
                    <a target="_blank" href="https://staff.skbkontur.ru/profile/tihonove">
                        @tihonove
                    </a>
                    .{" "}
                </Paragraph>
                <Header4>Важная информация:</Header4>
                <Paragraph>
                    Чтобы тесты были видны за пределами GitLab, надо, чтобы тесты попадали в артефакты, а не только в
                    JUnit report. (Подробнее см.{" "}
                    <a
                        target="_blank"
                        href="https://docs.gitlab.com/api/job_artifacts/#downloading-artifactsreports-files">
                        документацию GitLab
                    </a>
                    .)
                </Paragraph>
                <Paragraph>
                    Если ваш проект не открыт для всех контуровцев, необходимо будет выдать доступ учетной записи, под
                    которой работает сервис (уточним при добавлении).
                </Paragraph>
            </Modal.Body>
            <Modal.Footer panel>
                <Button onClick={props.onClose}>Закрыть</Button>
            </Modal.Footer>
        </Modal>
    );
}

const Paragraph = styled.p`
    margin-bottom: 20px;
    line-height: 24px;
`;

const Header4 = styled.p`
    margin-top: 40px;
    margin-bottom: 20px;
    font-weight: bold;
    line-height: 24px;
`;
