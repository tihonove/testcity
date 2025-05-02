import { uuidv4 } from "./Guids";

const USER_ID_KEY = "test-analytics-user-id";

export function getOrCreateUserId(): string {
    let userId = localStorage.getItem(USER_ID_KEY);
    
    if (!userId) {
        userId = uuidv4();
        localStorage.setItem(USER_ID_KEY, userId);
    }
    
    return userId;
}