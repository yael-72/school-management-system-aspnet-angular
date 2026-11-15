/** TodoTaskDto in the API. */
export interface TodoTask {
  id: number;
  title: string;
  /** ISO date "yyyy-MM-dd", or null. */
  dueDate: string | null;
  isDone: boolean;
  createdAt: string;
}

/** Body of POST /api/tasks and PUT /api/tasks/{id}. */
export interface SaveTodoTaskRequest {
  title: string;
  dueDate: string | null;
  isDone: boolean;
}
