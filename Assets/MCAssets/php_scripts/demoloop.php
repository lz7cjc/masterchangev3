<?php


$json = '{"data1":[{"Habit_ID":35,"label":"binary","amount":0},{"Habit_ID":34,"label":"binary","amount":0},{"Habit_ID":33,"label":"binary","amount":0}]}';

$userinfo = json_decode($json, true);



    foreach($userinfo["data1"] as $habit) {
        echo "ID: " . $habit["Habit_ID"] . "<br>";
        echo "label: " . $habit["label"] . "<br>";
        echo "amount: " . $habit["amount"] . "<br>";
    }


//echo "<br>jsoN looks like: " . $userinfo[0]->data1;
    
$conn->close();

?>