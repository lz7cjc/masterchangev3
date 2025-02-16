<?php
require("dbconnect.php");

//from Unity 
/*$userid = $_POST["user"];
$json = $_POST["cthearray"];*/


//pasted from unity 
$userid = 787;
/*
$json = '{"data1":[{"Habit_ID":35,"label":"binary","amount":0},{"Habit_ID":34,"label":"binary","amount":0},{"Habit_ID":33,"label":"binary","amount":0}]}';*/

$json =  '{"data1":[{"Habit_ID":35,"label":"binary","amount":1},{"Habit_ID":34,"label":"binary","amount":1},{"Habit_ID":33,"label":"binary","amount":0},{"Habit_ID":32,"label":"binary","amount":0},{"Habit_ID":29,"label":"binary","amount":0},{"Habit_ID":30,"label":"binary","amount":0},{"Habit_ID":31,"label":"binary","amount":1}]}';

$decoded = json_decode($json, true);

    foreach($decoded["data1"] as $habit) 
    {
        echo "ID: " . $habit["Habit_ID"] . "<br>";
        echo "label: " . $habit["label"] . "<br>";
        echo "amount: " . $habit["amount"] . "<br>";
        echo "<br>1<br>The Type is: habitid and the value is: <b> " . $habit["Habit_ID"] . "</b><br>"; 
       /* if ($habit["label"] = "binary")
        {
          "label is binary";
        }
    
          if ($habit["label"] == "habitid")
          {
                  $sthabitid = $value;
                echo "variable sthabitid:  " . $habit["Habit_ID"] . "<br>";
          }
          elseif ($habit["amount"]  == "amount")
          {
                echo "2b: second if statement: " . $key . "<br>";
                 $stvalue = $value;
                echo "variable stvalue:  " . $habit["amount"]  . "<br>";
 
          }
/*          elseif ($key == "label")
          {
                echo "2c: third if statement: " . $key . "<br>";
                 $stwhich = $value;
               echo "variable sttype:  " . $stwhich . "<br>";
 
          }}*/

      
      //check if record already exists
    $checkquery =  "SELECT * from userhabits where habit_id ='" . $habit["Habit_ID"]  . "' and user_id = '" . $userid . "';"; 
    echo "1 check. first query to see any records exist" . $checkquery . "<br>";
    $runCheck = mysqli_query($conn, $checkquery) or die("2:check query failed");

//if there is a record for that habit and that user, update
        if(mysqli_num_rows($runCheck) >0)
            {
	        echo "1: habit already exists";
    
          $updatevalue = "UPDATE userhabits SET yesorno = '" . $habit["amount"] . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $habit["Habit_ID"] . "';"; 
                echo "2a. Binary statement for update: " . $updatevalue . "<br>";

          mysqli_query($conn, $updatevalue) or die("1xxx: update failed "); //insert data or error for failure

            }
//if there is no record then insert
        else
            {
	           echo "3: new record";
             $inserthabitbinary = "INSERT INTO userhabits (user_id, habit_id, yesorno) VALUES ('" . $userid . "','" . $habit["Habit_ID"]  . "','" . $habit["amount"]  .  "');";
	               echo "3a insert binary values: " . $inserthabitbinary . "<br>";
                   mysqli_query($conn, $inserthabitbinary) or die("4: insert failed"); //insert data or error for failure
	           }



	echo "<br> ---------------------------------<br>End of insert/update for #age <br> ---------------------------------<br>";

    }
 echo  "<br>end of this inside loop <br><br>";
 
}

$conn->close();

?>
