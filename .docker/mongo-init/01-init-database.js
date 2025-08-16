// Script de inicialização do MongoDB
// Criação da database venice_orders_db e configuração inicial

print('Iniciando configuração do MongoDB...');

// Criar/conectar à database venice_orders_db
db = db.getSiblingDB('venice_orders_db');

// Criar a collection PedidoItens com validação
db.createCollection('PedidoItens', {
    validator: {
        $jsonSchema: {
            bsonType: "object",
            required: ["PedidoId", "ProdutoId", "Quantidade", "PrecoUnitario"],
            properties: {
                PedidoId: {
                    bsonType: "int",
                    description: "ID do pedido é obrigatório"
                },
                ProdutoId: {
                    bsonType: "int", 
                    description: "ID do produto é obrigatório"
                },
                Quantidade: {
                    bsonType: "int",
                    minimum: 1,
                    description: "Quantidade deve ser um número inteiro maior que 0"
                },
                PrecoUnitario: {
                    bsonType: "decimal",
                    description: "Preço unitário é obrigatório"
                },
                CreatedAt: {
                    bsonType: "date",
                    description: "Data de criação"
                },
                UpdatedAt: {
                    bsonType: "date",
                    description: "Data de atualização"
                }
            }
        }
    }
});

// Criar índices para otimização de consultas
db.PedidoItens.createIndex({ "PedidoId": 1 });
db.PedidoItens.createIndex({ "ProdutoId": 1 });
db.PedidoItens.createIndex({ "PedidoId": 1, "ProdutoId": 1 }, { unique: true });
db.PedidoItens.createIndex({ "CreatedAt": -1 });

print('Collection PedidoItens criada com índices otimizados');

// Inserir um documento de teste (opcional)
db.PedidoItens.insertOne({
    PedidoId: 0,
    ProdutoId: 0,
    Quantidade: 1,
    PrecoUnitario: NumberDecimal("0.01"),
    CreatedAt: new Date(),
    UpdatedAt: new Date(),
    _isTestDocument: true
});

print('Documento de teste inserido');

// Verificar se tudo foi criado corretamente
print('Collections na database venice_orders_db:');
db.runCommand("listCollections").cursor.firstBatch.forEach(
    function(collection) {
        print(' - ' + collection.name);
    }
);

print('Índices na collection PedidoItens:');
db.PedidoItens.getIndexes().forEach(
    function(index) {
        print(' - ' + index.name + ': ' + JSON.stringify(index.key));
    }
);

print('Configuração do MongoDB concluída com sucesso!');
