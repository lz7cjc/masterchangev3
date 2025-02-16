<?php
$to      = 'accounts@masterchange.co.uk';
$subject = 'MasterChange Registration: action required to complete';
$message = 'Almost there. You recently registered with MasterChange. To complete your registration, please respond to this email with your username. Failure to do this in the next 7 days will mean we will have to delete your account and you will lose all the information you have given us. This is to protect you from being wrongly registered. Thanks for your help in keeping the system clean!';
$headers = 'From: accounts@beriro.co.uk' . "\r\n" .
    'Reply-To: accounts@beriro.co.uk' . "\r\n" .
    'X-Mailer: PHP/' . phpversion();

// send email
$success = mail($EmailTo, $Subject, $Body, $headers);

// redirect to success page
if ($success){
  echo "sent successfully";
}
else{
   echo "not sent successfully";
}
?>