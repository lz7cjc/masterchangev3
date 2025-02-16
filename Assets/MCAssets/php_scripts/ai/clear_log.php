<?php
// Path to the error log file
$logFile = 'ai_content_errors.log';

// Clear the log file
file_put_contents($logFile, '');

echo json_encode(['status' => 'success', 'message' => 'Log file cleared']);
?>
