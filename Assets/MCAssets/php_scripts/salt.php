
<?php
//testing password hash salt
$password = "secret";
$password2 = "plsf";
 $hash = password_hash($password, PASSWORD_DEFAULT);
 echo $hash . "<br>";

 $checkpassword = password_verify($password2, $hash) ;
 echo "password check " . $checkpassword;

 
 ?>