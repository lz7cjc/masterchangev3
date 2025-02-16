<?php
require("dbconnect.php");

//from Unity 
$userid = $_POST["user"];
$behaviourid = $_POST["behaviourid"];


//pasted from unity 
/*$userid = "223";
 $behaviourid = "11";
*/

      //check if record already exists
      $checkquery =  "SELECT * from user_behaviour where behaviourtypeid ='" . $behaviourid . "' and userid = '" . $userid . "';"; 
   //    echo "1 check. first query to see any records exist" . $checkquery . "<br>";
      $runCheck = mysqli_query($conn, $checkquery) or die("2:check query failed");
//echo "runcheck sql: " . $checkquery;
    /*echo "<br>wwhat do i get as label value for if statement: " . $habit["label"] . "<br>";
    *///check if a value or yesno
               //if there is a record for that habit and that user, update
                if(mysqli_num_rows($runCheck) >0)
                    {
                 echo "1: label already exists"; 
                 
                  $updatevalue = "UPDATE user_behaviour SET interested = '1' WHERE userid = '" . $userid . "' AND   behaviourtypeid  = '" . $behaviourid . "';"; 
      //                  echo "2a. Binary statement for update: " . $updatevalue . "<br>";

                  mysqli_query($conn, $updatevalue) or die("1xxx: update failed "); //insert data or error for failure
                
                    }
                //if there is no record then insert
                else
                    {
                     echo "<br>2 binary: new record";
                     
                      $inserthabitbinary = "INSERT INTO user_behaviour ( behaviourtypeid , userid, interested) VALUES ('" . $behaviourid  . "','" .  $userid . "','1');";
       //         echo "3a insert binary values: " . $inserthabitbinary . "<br>";
                   mysqli_query($conn, $inserthabitbinary) or die("4: insert failed"); //insert data or error for failure
                     
                    }

        

$conn->close();


?>
