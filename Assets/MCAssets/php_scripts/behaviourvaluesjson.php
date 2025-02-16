<?php
require("dbconnect.php");

//from Unity 
$userid = $_POST["user"];
$json = $_POST["cthearray"];


//pasted from unity 
/*$userid = "223";
 $json = '{"data1":[{"Behaviour_ID":9,"yesorno":1},{"Behaviour_ID":2,"yesorno":1},{"Behaviour_ID":6,"yesorno":1},{"Behaviour_ID":1,"yesorno":0},{"Behaviour_ID":11,"yesorno":0},{"Behaviour_ID":5,"yesorno":0},{"Behaviour_ID":4,"yesorno":1},{"Behaviour_ID":8,"yesorno":0},{"Behaviour_ID":7,"yesorno":0},{"Behaviour_ID":10,"yesorno":1}]}';*/
$decoded = json_decode($json, true);



    foreach($decoded["data1"] as $behaviour) 
    {
    
      //check if record already exists
      $checkquery =  "SELECT * from user_behaviour where behaviourtypeid ='" . $behaviour["Behaviour_ID"]  . "' and userid = '" . $userid . "';"; 
   //    echo "1 check. first query to see any records exist" . $checkquery . "<br>";
      $runCheck = mysqli_query($conn, $checkquery) or die("2:check query failed");
//echo "runcheck sql: " . $checkquery;
    /*echo "<br>wwhat do i get as label value for if statement: " . $habit["label"] . "<br>";
    *///check if a value or yesno
               //if there is a record for that habit and that user, update
                if(mysqli_num_rows($runCheck) >0)
                    {
      //           echo "1: label already exists"; 
                 
                  $updatevalue = "UPDATE user_behaviour SET interested = '" . $behaviour["yesorno"] . "' WHERE userid = '" . $userid . "' AND   behaviourtypeid  = '" . $behaviour["Behaviour_ID"] . "';"; 
      //                  echo "2a. Binary statement for update: " . $updatevalue . "<br>";

                  mysqli_query($conn, $updatevalue) or die("1xxx: update failed "); //insert data or error for failure
                
                    }
                //if there is no record then insert
                else
                    {
                //     echo "<br>2 binary: new record";
                     
                      $inserthabitbinary = "INSERT INTO user_behaviour ( behaviourtypeid , userid, interested) VALUES ('" . $behaviour["Behaviour_ID"]  . "','" .  $userid . "','" . $behaviour["yesorno"]  .  "');";
       //         echo "3a insert binary values: " . $inserthabitbinary . "<br>";
                   mysqli_query($conn, $inserthabitbinary) or die("4: insert failed"); //insert data or error for failure
                     
                    }

        
}
// echo "get in back" . $newhabits; 


$conn->close();

?>
