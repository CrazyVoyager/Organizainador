
const {Pool} = require('pg');

const config = {
    user: 'postgres',
    host: 'localhost',
    password: '123',
    database: 'BD_org'
    }

    const pool = new Pool(config);

    const getusr = async()=>{

    try{
        const res= await pool.query('SELECT * FROM tab_usr')
        console.log(res.rows)
        pool.end();
    }catch(e){
        console.log(e)
    }
};

const insertusr = async()=>{

    try{
        const text =('Insert into tab_usr(tus_nom, tus_mail) VALUES($1, $2) RETURNING *')
        const values =['Usuario10','test@test.com']
        
        const res = await pool.query(text,values)
        console.log(res.rows)
        pool.end();
    }catch(e){
        console.log(e)
    }
};


const deleteusr = async()=>{

  
        const text =('DELETE FROM tab_usr WHERE tus_id_usr=$1')
        const values =['6']
        
        const res = await pool.query(text,values)
        console.log(res);
};

const editusr = async()=>{

  
        const text =('UPDATE tab_usr SET tus_nom = $1 WHERE tus_id_usr=$2')
        const values =['JUAN', '3']
        
        const res = await pool.query(text,values)
        console.log(res);
};

//editusr();
//deleteusr();
//insertusr();
//getusrs();
