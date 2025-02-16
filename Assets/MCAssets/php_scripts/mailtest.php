<?php
$to = "nick@beriro.co.uk";
$subject = "This is a mail test";
$message = "Hello! This is a simple email message.";
$from = "passwordreset@masterchange.today";
$headers = "From: " . $from;
mail($to,$subject,$message,$headers);
echo "Mail Sent.";
?>
