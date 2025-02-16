<?php

//DB connection - change in included file
require("dbconnect.php");


//from Unity 
$userid = $_POST["user"];
$json = $_POST["drinkArray"];
$dateofdrink = $_POST["daysago"];

//pasted from unity 
/*$userid = "16589459716";
$json =  '{"data1":[{"drinkName":1,"drinkAmount":"2","label":"drink"},{"drinkName":2,"drinkAmount":"3","label":"drink"},{"drinkName":3,"drinkAmount":"1","label":"drink"},{"drinkName":4,"drinkAmount":"0","label":"drink"},{"drinkName":5,"drinkAmount":"0","label":"drink"},{"drinkName":6,"drinkAmount":"0","label":"drink"},{"drinkName":7,"drinkAmount":"0","label":"drink"}]}';
$dateofdrink = "3";*/
 


// echo  $userid;
// echo $json;
// echo $dateofdrink;

//GO
//Pass the date you want to subtract from in as
//the $time parameter for DateTime.
$currentDate = new DateTime();
$offset = "P".$dateofdrink."D";
//echo $offset;
 
//Subtract a day using DateInterval
$consumedDate = $currentDate->sub(new DateInterval($offset));
 
//Get the date in a YYYY-MM-DD format.
$consumedDate = $consumedDate->format('Y-m-d');
//Print it out.
//echo $consumedDate . "<br>";

    $decoded = json_decode($json, true);
    foreach($decoded["data1"] as $habit) 
       {
          //check if record already exists
          $checkquery =  "Select userid, dateofdrink, drinkid From drinkusage where userid ='" . $userid . "' and dateofdrink = '" . $consumedDate . "' and drinkid ='" . $habit["drinkName"] . "';"; 
         // echo $checkquery;
         // echo "checking query is " . $checkquery;
          $runCheck = mysqli_query($conn, $checkquery) or die("2:check query failed");
    if(mysqli_num_rows($runCheck) >0)
          {
                      $reward = "0";
                      $updatevalue = "UPDATE drinkusage SET amount = '" . $habit["drinkAmount"] . "' WHERE userid = '" . $userid . "' AND drinkid = '" . $habit["drinkName"] . "' AND dateofdrink = '" . $consumedDate . "';"; 
                    // echo "updating" . $updatevalue . "<br>";
                      mysqli_query($conn, $updatevalue) or die("4: update drink tracker failed"); //insert data or error for failure
//echo "updated";
          }
      else
          {     
  //echo "<br>2 binary: new record";
           $insertdrinkday = "INSERT INTO drinkusage(drinkid, userid, amount, dateofdrink) VALUES ('" . $habit["drinkName"] . "','" . $userid . "','" . $habit["drinkAmount"] . "','" . $consumedDate . "');" ;
//echo "inserting " . $insertdrinkday;
          mysqli_query($conn, $insertdrinkday) or die("4: insert drink tracker failed"); //insert data or error for failure
                  
          
                   $reward = "1";
                  
      }           
  } 

echo '{"reward":' . $reward . '}';

$conn->close();

?>
