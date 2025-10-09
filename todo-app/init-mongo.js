// Initialize MongoDB database with sample todos
db = db.getSiblingDB('ToDoAppDb');

db.TodoItems.insertMany([
    {
        "Id": 1,
        "Name": "Learn Kubernetes",
        "IsComplete": false
    },
    {
        "Id": 2,
        "Name": "Deploy MongoDB",
        "IsComplete": true
    }
]);

print("Database initialized with sample todos!");