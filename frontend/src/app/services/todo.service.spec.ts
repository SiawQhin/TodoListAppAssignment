import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TodoService } from './todo.service';
import { TodoItem, CreateTodoRequest, UpdateTodoRequest } from '../models/todo-item.model';

describe('TodoService', () => {
  let service: TodoService;
  let httpMock: HttpTestingController;
  const apiUrl = 'http://localhost:5000/api/todos';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TodoService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(TodoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAll', () => {
    it('should return an array of todos', () => {
      const mockTodos: TodoItem[] = [
        {
          id: '1',
          title: 'Test Todo 1',
          isCompleted: false,
          createdAt: '2025-01-18T00:00:00Z'
        },
        {
          id: '2',
          title: 'Test Todo 2',
          isCompleted: true,
          createdAt: '2025-01-18T00:00:00Z'
        }
      ];

      service.getAll().subscribe(todos => {
        expect(todos).toEqual(mockTodos);
        expect(todos.length).toBe(2);
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockTodos);
    });

    it('should return an empty array when no todos exist', () => {
      service.getAll().subscribe(todos => {
        expect(todos).toEqual([]);
        expect(todos.length).toBe(0);
      });

      const req = httpMock.expectOne(apiUrl);
      req.flush([]);
    });
  });

  describe('getById', () => {
    it('should return a single todo', () => {
      const mockTodo: TodoItem = {
        id: '1',
        title: 'Test Todo',
        isCompleted: false,
        createdAt: '2025-01-18T00:00:00Z'
      };

      service.getById('1').subscribe(todo => {
        expect(todo).toEqual(mockTodo);
        expect(todo.id).toBe('1');
        expect(todo.title).toBe('Test Todo');
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockTodo);
    });
  });

  describe('create', () => {
    it('should create a new todo', () => {
      const newTodoRequest: CreateTodoRequest = {
        title: 'New Todo'
      };

      const createdTodo: TodoItem = {
        id: '3',
        title: 'New Todo',
        isCompleted: false,
        createdAt: '2025-01-18T00:00:00Z'
      };

      service.create(newTodoRequest).subscribe(todo => {
        expect(todo).toEqual(createdTodo);
        expect(todo.id).toBeDefined();
        expect(todo.title).toBe('New Todo');
        expect(todo.isCompleted).toBe(false);
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newTodoRequest);
      req.flush(createdTodo);
    });
  });

  describe('update', () => {
    it('should update an existing todo', () => {
      const updateRequest: UpdateTodoRequest = {
        title: 'Updated Todo',
        isCompleted: true
      };

      const updatedTodo: TodoItem = {
        id: '1',
        title: 'Updated Todo',
        isCompleted: true,
        createdAt: '2025-01-18T00:00:00Z'
      };

      service.update('1', updateRequest).subscribe(todo => {
        expect(todo).toEqual(updatedTodo);
        expect(todo.title).toBe('Updated Todo');
        expect(todo.isCompleted).toBe(true);
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateRequest);
      req.flush(updatedTodo);
    });
  });

  describe('delete', () => {
    it('should delete a todo', () => {
      service.delete('1').subscribe(() => {
        // Delete successful
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  afterEach(() => {
    httpMock.verify();
  });
});
