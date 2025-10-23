
const {Pool} = require('pg');

const config = {
    user: 'postgres',
    host: 'localhost',
    password: '123',
    database: 'BD_org'
    }

    const pool = new Pool(config);