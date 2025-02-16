<!-- test_interface.php -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Content Generation Test Interface</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 20px auto; padding: 0 20px; }
        .test-form { background: #f5f5f5; padding: 20px; border-radius: 8px; margin-bottom: 20px; }
        .form-group { margin-bottom: 15px; }
        label { display: block; margin-bottom: 5px; }
        input[type="number"], input[type="text"] { width: 100%; padding: 8px; margin-bottom: 10px; }
        button { background: #007bff; color: white; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer; }
        button:hover { background: #0056b3; }
        #results { white-space: pre-wrap; background: #f8f9fa; padding: 15px; border-radius: 4px; }
        .content-type { display: flex; align-items: center; margin-bottom: 5px; }
        .content-type input { width: 60px; margin-right: 10px; }
        #contentTypes { margin-bottom: 15px; }
        .remove-type { background: #dc3545; padding: 5px 10px; margin-left: 10px; }
    </style>
</head>
<body>
    <h1>Content Generation Test Interface</h1>
    
    <div class="test-form">
        <div class="form-group">
            <label for="userId">User ID:</label>
            <input type="number" id="userId" value="1" min="1">
        </div>
        
        <div class="form-group">
            <label>Content Types:</label>
            <div id="contentTypes">
                <div class="content-type">
                    <input type="number" value="2" min="1" max="4">
                    <span>Type 1</span>
                    <button class="remove-type">Remove</button>
                </div>
            </div>
            <button onclick="addContentType()">Add Content Type</button>
        </div>
        
        <button onclick="runTest()">Run Test</button>
    </div>

    <div>
        <h2>Test Results:</h2>
        <pre id="results">Results will appear here...</pre>
    </div>

    <script>
        function addContentType() {
            const container = document.getElementById('contentTypes');
            const typeCount = container.children.length + 1;
            
            const div = document.createElement('div');
            div.className = 'content-type';
            div.innerHTML = `
                <input type="number" value="2" min="1" max="4">
                <span>Type ${typeCount}</span>
                <button class="remove-type">Remove</button>
            `;
            
            container.appendChild(div);
        }

        document.getElementById('contentTypes').addEventListener('click', function(e) {
            if (e.target.className === 'remove-type') {
                e.target.parentElement.remove();
            }
        });

        async function runTest() {
            const userId = document.getElementById('userId').value;
            const contentTypes = Array.from(
                document.querySelectorAll('#contentTypes input')
            ).map(input => parseInt(input.value));

            const results = document.getElementById('results');
            results.textContent = 'Running test...';

            try {
                const response = await fetch('test_content_generation.php', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        userId: userId,
                        contentTypes: contentTypes
                    })
                });

                const data = await response.text();
                results.textContent = data;
            } catch (error) {
                results.textContent = 'Error running test: ' + error.message;
            }
        }
    </script>
</body>
</html>